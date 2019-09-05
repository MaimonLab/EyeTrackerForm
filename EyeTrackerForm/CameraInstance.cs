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
        public int mRoiLeft = 10;
        public int mRoiRight = 20;
        public int mThreshold = 200;
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


        // Use the following enum and global static variable to select the type
        // of video file to be created and saved.
        enum VideoType
        {
            Uncompressed,
            Mjpg,
            H264
        }

        static VideoType mChosenFileType = VideoType.Mjpg;

        IManagedSpinVideo mTimelapseVid;
        IManagedSpinVideo mFeedingVid;

        //public bool lockTaken = false;



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

//            Change framerate
            IFloat iAcquisitionFrameRate = camMap.GetNode<IFloat>("AcquisitionFrameRate");
            iAcquisitionFrameRate.Value = 30.0;
            mFrameRate = (float) iAcquisitionFrameRate.Value;

            mTimelapseVid = new ManagedSpinVideo();
            mTimelapseVid.SetMaximumFileSize(8192);

            mFeedingVid = new ManagedSpinVideo();
            mFeedingVid.SetMaximumFileSize(8192);

            serialNumber = mCamera.DeviceSerialNumber.ToString();


            float frameRateToSet = 30;
            string timelapseFilename = "test_timelapse_01" + serialNumber;
            string feedFilename = "test_feed" + serialNumber;

            //timelapseInterval = 15;

            switch (mChosenFileType)
            {
                case VideoType.Uncompressed:
                    AviOption uncompressedOption = new AviOption();
                    uncompressedOption.frameRate = frameRateToSet;
                    mTimelapseVid.Open(timelapseFilename, uncompressedOption);
                    mFeedingVid.Open(feedFilename, uncompressedOption);
                    break;

                case VideoType.Mjpg:
                    MJPGOption mjpgOption = new MJPGOption();
                    mjpgOption.frameRate = frameRateToSet;
                    mjpgOption.quality = 75;
                    mTimelapseVid.Open(timelapseFilename, mjpgOption);
                    mFeedingVid.Open(feedFilename, mjpgOption);
                    break;

                //case VideoType.H264:
                //    H264Option h264Option = new H264Option();
                //    h264Option.frameRate = frameRateToSet;
                //    h264Option.bitrate = 1000000;
                //    h264Option.height = Convert.ToInt32(image.Height);
                //    h264Option.width = Convert.ToInt32(image.Width);
                //    mTimelapseVid.Open(videoFilename, h264Option);
                //    break;
            }


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

            while(mStillAlive)
            {
                try
                {


                    IManagedImage image = mCamera.GetNextImage(1000);
                    double imageTime = ConverTime(image.ChunkData.Timestamp);
                    if (logger.IsDebugEnabled)
                    {
                        logger.Debug("FrameTime Frame {0} at {1}", image.ChunkData.FrameID, imageTime);

                        logger.Debug("Recived Frame {0} at {1}", image.ChunkData.FrameID, HighResolutionDateTime.UtcNow);
                    }
                    //logger.Debug("size of event queue is {0}", mEventQueue.Count);
                    Image<Gray, Byte> myImage = new Image<Gray, Byte>((int)image.Width, (int)image.Height, (int)image.Stride, image.DataPtr);
                    FrameData currentFrame = new FrameData(image.ChunkData.FrameID, myImage);

                    currentFrame.ImageTime = imageTime;

                    // If we're recording we'll only display and record the timelapse frame, otherwise we'll display all and record none.
                   
                    if (currentFrame.FameID % mTimelapseInterval == 0 || !mRecord)
                    {
                        if (mDisplay) //mDisplay
                        {
                            FrameData displayFrame;
                            displayFrame = new FrameData(currentFrame.FameID, currentFrame.Image);
                            mDisplayQueue.Add(displayFrame);
                        }

                        if (mRecord)
                        {
                            mTimelapseVid.Append(image);
                        }
                        
                    }
                    
                    if(mRecord && mFeedFrameCountDown > 0)
                    {
                        mFeedingVid.Append(image);
                        mFeedFrameCountDown--;
                        Console.WriteLine("writing feed frame");
                    }
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
                                logger.Debug("Display Frame {0} at {1}", dispFrame.FameID, HighResolutionDateTime.UtcNow);
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

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Watcher(string filename)
        {
            mWatcher = new FileSystemWatcher();
            
            mWatcher.Path = Path.GetDirectoryName(filename);
            Console.WriteLine("starting watching with path {0}", mWatcher.Path);

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            mWatcher.NotifyFilter = NotifyFilters.LastAccess
                                    | NotifyFilters.LastWrite
                                    | NotifyFilters.FileName
                                    | NotifyFilters.DirectoryName;

            // Only watch text files.
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
            Console.WriteLine("starting feeding video save for {0} frames", mFeedFrameCountDown);


        }

        public void ROIChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mTimelapseInterval = page.mTimelapseTrackBar.mTrackbar.Value;
            mFeedVidLength = page.mFeedVidLengthTrackBar.mTrackbar.Value;
            mRoiLeft = page.mLeftTrackBar.mTrackbar.Value;
            mRoiRight = page.mRightTrackBar.mTrackbar.Value;
            mThreshold = page.mThresholdTrackBar.mTrackbar.Value;
        }

        public void RecordChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mRecord = page.mRecord.Checked;
            if (mRecord)
            {
                logger.Info("Pupil recodings stared on camera {5} with the following timelapse, BOTTOM, LEFT, RIGHT, THRESHOLD values: {0}, {1}, {2}, {3}, {4}",
                    mTimelapseInterval, mFeedVidLength, mRoiLeft, mRoiRight, mThreshold, mCamera.DeviceSerialNumber.ToString());

                OpenFileDialog openFileDialog1 = new OpenFileDialog();
                openFileDialog1.InitialDirectory = "C:\\Users\\maimon\\Documents\\MATLAB\\experiment_outputs";

                DialogResult result = openFileDialog1.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(openFileDialog1.FileName)) // Test result.
                {
                    mWatchPath = openFileDialog1.FileName;
                    mWatching = true;
                    Watcher(mWatchPath);
                    page.mPathToWatchBox.Text = mWatchPath;
                }
                //Console.WriteLine(file); // <-- For debugging use.
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

            mTimelapseVid.Close();
            mFeedingVid.Close();

            mCamera.EndAcquisition();
            mCamera.Dispose();
        }


    }

    public class FrameData : Object
    {
        public long FameID { get; set; }
        public Image<Gray, Byte> Image { get; set; }
        public double ImageTime { get; set; }

        public FrameData() { }


        public FrameData(long frameId, Image<Gray, Byte> image)
        {
            this.FameID = frameId;
            this.Image = image;
            //this.IImage = iimage;


        }


    }

}
