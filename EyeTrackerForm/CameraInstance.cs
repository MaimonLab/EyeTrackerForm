using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using SpinnakerNET.Video;

using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Security.Permissions;

using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Concurrent;

namespace EyeTrackerForm
{
    public class CameraInstance
    {
        private ImageEventListener mImageEventListener;
        private IManagedCamera mCamera;
        private CameraComponent mComponent;
        public int mTimelapseInterval = 15;
        public int mFeedVidLength = 10;
        public int mDisplayInterval = 10;

        public bool mFullImage = true;
        public bool mRecord;
        public bool mDisplay;
        public Pen mPen;
        public bool mStillAlive = true;
        private int counter = 0;
        public double cam2sys = 0.0;
        public double lastSystemTime = 0;
        public long lastCameraTime = 0;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public BlockingCollection<FrameData> mDisplayQueue;
        public BlockingCollection<IManagedImage> mEventQueue;
        public Thread mTimeModel;
        public Thread mGrabThread;
        public Thread mDisplayProcessThread;
        public Thread mEventThread;
        public string serialNumber;


        public string mWatchPath;
        public bool mWatching = false;
        public int mFeedFrameCountDown = 0;
        public float mFrameRate;
        public FileSystemWatcher mWatcher;
        
        ImprovedVideoWriter mTimelapseVid;
        ImprovedVideoWriter mFeedingVid;

        public CameraInstance(IManagedCamera camera, CameraComponent component)
        {
            // Workers

            mDisplayQueue = new BlockingCollection<FrameData>();
            mEventQueue = new BlockingCollection<IManagedImage>();

            mDisplayProcessThread = new Thread(this.DoDisplayImage);
            mDisplayProcessThread.Start();
            //mTimeModel = new Thread(this.HandleImageEvent);
            //mTimeModel.Start();
            mGrabThread = new Thread(this.DoCameraGrab);
            mGrabThread.Start();

            mComponent = component;
            mCamera = camera;
            mCamera.Init();
            INodeMap camMap = mCamera.GetNodeMap();

            IBool iChunkModeActive = camMap.GetNode<IBool>("ChunkModeActive");
            iChunkModeActive.Value = true;
            IEnum iChunkSelector = camMap.GetNode<IEnum>("ChunkSelector");
            EnumEntry[] entries = iChunkSelector.Entries;

            iChunkSelector.Value = 14;
            IBool iChunkEnableTime = camMap.GetNode<IBool>("ChunkEnable");
            iChunkEnableTime.Value = true;

            iChunkSelector.Value = 2;
            IBool iChunkEnableFrame = camMap.GetNode<IBool>("ChunkEnable");
            iChunkEnableFrame.Value = true;

            // Turn off auto framerate if doable.
            IEnum iAcquisitionFrameRateAuto = camMap.GetNode<IEnum>("AcquisitionFrameRateAuto");
            if (iAcquisitionFrameRateAuto == null || !iAcquisitionFrameRateAuto.IsWritable)
            {
                Console.WriteLine("Unable to disable automatic framerate (enum retrieval). Aborting...\n");
                return;
            }

            IEnumEntry iExposureAutoOff = iAcquisitionFrameRateAuto.GetEntryByName("Off");
            if (iExposureAutoOff == null || !iExposureAutoOff.IsReadable)
            {
                Console.WriteLine("Unable to disable automatic framerate (entry retrieval). Aborting...\n");
                return;
            }

            iAcquisitionFrameRateAuto.Value = iExposureAutoOff.Value;

            //     Change framerate

            IFloat iAcquisitionFrameRate = camMap.GetNode<IFloat>("AcquisitionFrameRate");
            iAcquisitionFrameRate.Value = 30.0;
            mFrameRate = (float) iAcquisitionFrameRate.Value;
            
            serialNumber = mCamera.DeviceSerialNumber.ToString();


            Thread.Sleep(300);
            mTimeModel = new Thread(this.TimeModel);
            mTimeModel.Start();
            //Thread.Sleep(30);
            mCamera.BeginAcquisition();

        }

        public int Xchannel { get; set; }
        public int Ychannel { get; set; }


        ~CameraInstance()
        {

        }

        public void DoCameraGrab()
        {

            Thread.Sleep(500);
            // Parameters for Timestamp applied to image
            Rectangle TimestampBackground = new Rectangle(2, (int)mCamera.Height.Value - 25, 325, 25);
            Gray TimestampBackgroundColor = new Gray(0);
            System.Drawing.Point TimeStampLocation = new System.Drawing.Point(2, (int)mCamera.Height.Value - 2);
            Gray TimeStampTextColor = new Gray(255);


            while (mStillAlive)
            {
                
                try
                {

                    // Grab image off of camera buffer (on PC side). wait max 1 second for image.
                    IManagedImage image = mCamera.GetNextImage(1000);
                    double imageTime = ConverTime(image.ChunkData.Timestamp);
                    if (logger.IsDebugEnabled)
                    {
                        logger.Debug("FrameTime Frame {0} at {1}", image.ChunkData.FrameID, imageTime);

                        logger.Debug("Recived Frame {0} at {1}", image.ChunkData.FrameID, HighResolutionDateTime.UtcNow);
                    }

                    //Create openCV image from Spinnaker IManagedImage. Spinnaker Image will be free to release
                    Image<Gray, Byte> myImage = new Image<Gray, Byte>((int)image.Width, (int)image.Height, (int)image.Stride, image.DataPtr);

                    //Grab meta data
                    FrameData currentFrame = new FrameData(image.ChunkData.FrameID, myImage);
                    currentFrame.ImageTime = imageTime;

                    //Create timestamp on image
                    myImage.Draw(TimestampBackground, TimestampBackgroundColor, -1);
                    myImage.Draw(DateTime.Now.ToString("MM/dd/yy HH:mm:ss"), TimeStampLocation, Emgu.CV.CvEnum.FontFace.HersheyPlain, 2.0, TimeStampTextColor, 2, Emgu.CV.CvEnum.LineType.EightConnected);

                    // If we're recording we'll only record the timelapse frame once every mTimelapseInterval
                    if (currentFrame.FrameID % mTimelapseInterval == 0  && mRecord)
                    {
                        mTimelapseVid.Write(myImage.Mat);

                    }

                    // if we're displaying then either dislay every frame (if not recording) or display one frame every mDisplayInterval (if not.)
                    if (mDisplay)
                        {
                          if (!mRecord || currentFrame.FrameID % mDisplayInterval == 0 )
                          {
                              FrameData displayFrame;
                              displayFrame = new FrameData(currentFrame.FrameID, currentFrame.Image);
                              mDisplayQueue.Add(displayFrame);
                          }

                        }

                    //if we're recording and there is a mFeedFrameCountdown, record every frame until we've gone through that countdown.
                    if(mRecord && mFeedFrameCountDown > 0)
                    {
                        mFeedingVid.Write(myImage.Mat);
                        mFeedFrameCountDown--;
                    }

                    //TODO: Dispose of image as soon as possible.
                    //      Create no blocking queues for all process as not to block camera grab 
                    image.Dispose();


                }
                catch
                {
                    Thread.Sleep(7);
                }

            }
        }





        public void DoDisplayImage(object img)
        {

            FrameData dispFrame;

            while (mStillAlive)
            {
                try
                {
                    dispFrame = mDisplayQueue.Take();
                    Image<Gray, Byte> image = dispFrame.Image;

                    var _object = new object();
                    bool lockTaken = false;
                    try
                    {
                        logger.Debug("size of display queue is {0}", mDisplayQueue.Count);
                        Monitor.TryEnter(_object, 5, ref lockTaken);

                        if (lockTaken)
                        {


                            mComponent.HandleDisplayImage(image);

                            if (logger.IsDebugEnabled)
                            {
                                logger.Debug("Display Frame {0} at {1}", dispFrame.FrameID, HighResolutionDateTime.UtcNow);
                            }

                        }


                        //mComponent.HandleDisplayImage(workingImage.);

                        else
                        {
                            // The lock was not acquired.
                        }
                    }
                    finally
                    {
                        // Ensure that the lock is released.
                        if (lockTaken)
                        {
                            Monitor.Exit(_object);
                        }
                        else
                        {
                        }
                    }

                }
                catch
                {
                    Thread.Sleep(50);
                }
            }



        }


        public void TimeModel()
        {




            while (mStillAlive)
            {
                //mCamera.Timestamp
                mCamera.TimestampLatch.Execute();
                double thisTime = HighResolutionDateTime.UtcNow;
                //Thread.Sleep(300);
                long cameraTime = mCamera.Timestamp;


                if (lastSystemTime == 0)
                {
                    lastSystemTime = thisTime;
                    lastCameraTime = cameraTime;
                    Thread.Sleep(16);

                }
                else
                {
                    long camDuration = cameraTime - lastCameraTime;
                    double sysDuration = thisTime - lastSystemTime;
                    cam2sys = camDuration / sysDuration;
                    lastCameraTime = cameraTime;
                    lastSystemTime = thisTime;
                    Thread.Sleep(5000);

                }



            }
        }

        public double ConverTime (long camTime)
        {
            return (((camTime - lastCameraTime) / cam2sys)+ lastSystemTime);

        }
        //Starts  filesystem watching at the chosen path to trigger feeding recordings.
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Watcher(string watchpath)
        {
            mWatcher = new FileSystemWatcher();

            mWatcher.Path = watchpath;
            logger.Info("starting watching with path {0}", mWatcher.Path);

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            mWatcher.NotifyFilter = NotifyFilters.LastAccess
                                    | NotifyFilters.LastWrite
                                    | NotifyFilters.FileName
                                    | NotifyFilters.DirectoryName;

            // Only watch csv files.
            mWatcher.Filter = "*.csv";

            // Add event handlers.
            mWatcher.Changed += OnChanged;
            mWatcher.Created += OnChanged;
            mWatcher.Deleted += OnChanged;

            // Begin watching.
            mWatcher.EnableRaisingEvents = true;
        }




        // Define the event handlers.
        public  void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            mFeedFrameCountDown = (int)Math.Round( mFrameRate * mFeedVidLength);
            logger.Info("starting feeding video save for {0} frames", mFeedFrameCountDown);


        }

        public void ROIChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mTimelapseInterval = page.mTimelapseTrackBar.mTrackbar.Value;
            mDisplayInterval = page.mDisplayIntervalTrackBar.mTrackbar.Value;
            mFeedVidLength = page.mFeedVidLengthTrackBar.mTrackbar.Value;

        }

        public void RecordChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mRecord = page.mRecord.Checked;
            if (mRecord)
            {

                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.InitialDirectory = "C:\\Users\\maimon\\Documents\\MATLAB\\experiment_outputs";

                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog1.FileName)) // Test result.
                {
                    string filename = openFileDialog1.FileName;
                    mWatchPath = Path.GetDirectoryName(filename);
                    mWatching = true;
                    Watcher(mWatchPath);

                    page.mPathToWatchBox.Text = mWatchPath;
                }

                logger.Info("Recording started on camera {3} with the following timelapse interval, feedvideoLength, and watchpath: {0}, {1}, {2}",
                    mTimelapseInterval, mFeedVidLength, mWatchPath, mCamera.DeviceSerialNumber.ToString());

                string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string rigLable = mWatchPath.Split(Path.DirectorySeparatorChar).Last();
                string vidpath = Path.Combine(homePath, "flyVideos", rigLable);

                //Check that output directory exists
                if (!Directory.Exists(vidpath))
                {
                    Directory.CreateDirectory(vidpath);
                    logger.Info("Output directory did not exist. Creating output directory {0}", vidpath);
                }
                else
                {
                    logger.Info("Output directory: {0}", vidpath);
                }

                // Create video file paths
                string timelapseFilename = (vidpath + Path.DirectorySeparatorChar + "timelapse_" + serialNumber + DateTime.Now.ToString("_yyyy_MM_dd_hh_mm_ss"));
                string feedFilename = vidpath + Path.DirectorySeparatorChar + "feeding_" + serialNumber + DateTime.Now.ToString("_yyyy_MM_dd_hh_mm_ss");
                
                // Create Video Writers
                mTimelapseVid = new ImprovedVideoWriter(timelapseFilename, ImprovedVideoWriter.VideoCompressionType.H264, 20.0, (int)mCamera.Width.Value, (int)mCamera.Height.Value, true);
                mTimelapseVid.MaxFileSize = 8000;
                mFeedingVid = new ImprovedVideoWriter(feedFilename, ImprovedVideoWriter.VideoCompressionType.H264, 20.0, (int)mCamera.Width.Value, (int)mCamera.Height.Value, true);
                mFeedingVid.MaxFileSize = 8000;

            }
        }

        public void FullImageChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mFullImage = page.mFullImage.Checked;
        }

        public void DisplayChangeHandler(object sender, System.EventArgs e)
        {

        }

        public void Close()
        {
            mStillAlive = false;
            Thread.Sleep(100);
            if (mGrabThread.IsAlive)
            {
                mGrabThread.Abort();
            }

            if (mDisplayProcessThread.IsAlive)
            {
                mDisplayProcessThread.Abort();
            }
            if (mTimeModel.IsAlive)
            {
                mTimeModel.Abort();
            }

            mCamera.EndAcquisition();
            mCamera.Dispose();
        }


    }

    public class FrameData : Object
    {
        public long FrameID { get; set; }
        public Image<Gray, Byte> Image { get; set; }
        public double ImageTime { get; set; }

        public FrameData() { }


        public FrameData(long frameId, Image<Gray, Byte> image)
        {
            this.FrameID = frameId;
            this.Image = image;
            //this.IImage = iimage;


        }


    }

}
