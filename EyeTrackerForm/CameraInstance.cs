using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpinnakerNET;
using SpinnakerNET.GenApi;

using System.Drawing;
using System.Windows;
using System.Threading;
using System.IO;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Concurrent;
using CsvHelper.Configuration;
using CsvHelper;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Configuration;

namespace EyeTrackerForm
{
    public class CameraInstance
    {
        private ImageEventListener mImageEventListener;
        private IManagedCamera mCamera;
        private CameraComponent mComponent;
        public int mRoiTop = 200;
        public int mRoiBottom = 400;
        public int mRoiLeft = 600;
        public int mRoiRight = 800;
        public int mThreshold = 200;
        public bool mFullImage = true;
        public bool mThreshImage = false;
        public bool mRecord;
        public bool mDisplay;
        public Pen mPen;
        public bool mStillAlive = true;
        public double cam2sys = 0.0;
        public double lastSystemTime = 0;
        public long lastCameraTime = 0;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public BlockingCollection<FrameData> mDisplayQueue;
        public BlockingCollection<FrameData> mPupilQueue;
        public BlockingCollection<IManagedImage> mEventQueue;
        public Thread mTimeModel;
        public Thread mGrabThread;
        public Thread mPupilProcessThread;
        public Thread mDisplayProcessThread;
        public Thread mEventThread;
        public string serialNumber;

        public event EventHandler<LatencyEventArgs> LatencyEvent;

        public StreamWriter writer;
        public CsvWriter mDataFile;

        ImprovedVideoWriter mTimelapseVid;
        //ImprovedVideoWriter mThreshVid;
        public int mTimelapseInterval = 1;
        public bool mRunning = false;
        //public bool lockTaken = false;




        public CameraInstance(IManagedCamera camera, CameraComponent component)
        {
            // Workers

            mDisplayQueue = new BlockingCollection<FrameData>();
            mPupilQueue = new BlockingCollection<FrameData>();
            mEventQueue = new BlockingCollection<IManagedImage>();
            mPupilProcessThread = new Thread(this.DoPupilTracking);
            mPupilProcessThread.Priority = ThreadPriority.Highest;
            mPupilProcessThread.Start();
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


            //mImageEventListener = new ImageEventListener(mCamera, this, mEventQueue);
            //mImageEventListener.NewImageEvent += HandleImageEvent;
            //mCamera.RegisterEvent(mImageEventListener);
            //mCamera.ChunkEnable.
            Thread.Sleep(300);
            mTimeModel = new Thread(this.TimeModel);
            mTimeModel.Start();
            //Thread.Sleep(30);
            serialNumber = mCamera.DeviceSerialNumber.ToString();

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
                    FrameData pupilFrame = new FrameData(image.ChunkData.FrameID, myImage);
                    pupilFrame.ImageTime = imageTime;
                    image.Dispose();
                    mPupilQueue.Add(pupilFrame);
                }
                catch
                {
                    Thread.Sleep(7);
                }

            }
        }



        public void HandleImageEvent()
        {
            IManagedImage image;
            //logger.Debug("image {0} image time is {1}", e.image.ChunkData.CounterValue, e.image.ChunkData.Timestamp);
            while (mStillAlive)
            {
                try
                {
                    image = mEventQueue.Take();
                    logger.Debug("size of event queue is {0}", mEventQueue.Count);
                    Image<Gray, Byte> myImage = new Image<Gray, Byte>((int)image.Width,(int)image.Height, (int)image.Stride, image.DataPtr);
                    //CvInvoke.cvCopy(image.DataPtr,)
                    //Image<Gray, Byte> myImage = new Image<Gray, Byte>(image.bitmap);
                    //Image<Gray, Byte> myImage2 = new Image<Gray, Byte>(image.bitmap);
                    //FrameData displayFrame = new FrameData(image.ChunkData.FrameID, myImage);
                    FrameData pupilFrame = new FrameData(image.ChunkData.FrameID, myImage);
                    image.Dispose();
                    //Image<Gray, Byte> myImage = new Image<Gray, Byte>(e.image.bitmap);
                    //Thread displayThread = new Thread(this.DoDisplayImage);
                    //displayThread.Start(myImage2);

                    mPupilQueue.Add(pupilFrame);
                    //mDisplayQueue.Enqueue(displayFrame);
                }
                catch
                {
                    Thread.Sleep(7);
                }

            }

            //Thread cvThread = new Thread(this.DoPupilTracking);
            //cvThread.Start(pupilFrame);

            //cvThread.Join();
            //displayThread.Join();
            
        }

/*        public bool ContourCloseToEdge(VectorOfPoint contour, int imageWidth, int imageHeight,
                                        int minDist = 5)
        {
            System.Drawing.Rectangle boundingBox = CvInvoke.BoundingRectangle(contour);
            int xMin, yMin, xMax, yMax;
            xMin = yMin = minDist;
            xMax = imageWidth - minDist;
            yMax = imageHeight - minDist;

            if (boundingBox.X <= xMin || boundingBox.Y <= yMin ||
               boundingBox.X + boundingBox.Width >= xMax ||
               boundingBox.Y + boundingBox.Height >= yMax)
            {
                return true;
            }
            else return false;
        }*/

        public void DoPupilTracking()
        {

            double pupx;
            double pupy;
            FrameData thisFrame;

            Image<Gray, Byte> image;
            /* Image<Gray, Byte> thresImage;
             Image<Gray, Byte> blurImag;
             VectorOfVectorOfPoint contours;
             Image<Gray, Byte> cropImg;
             */
            while (mStillAlive)
            {
                try
                {
                    thisFrame = mPupilQueue.Take();
                    //logger.Debug("size of pupil queue is {0}", mPupilQueue.Count);
                    image = thisFrame.Image;
                   /* thresImage = image;
                    cropImg = image.Copy(new Rectangle(mRoiLeft, mRoiTop, mRoiRight - mRoiLeft, mRoiBottom - mRoiTop));
                 */   if (mRecord)
                    {

                        /*  try
                          {

                             *//* blurImag = cropImg.SmoothBlur(5, 5);
                              blurImag._EqualizeHist();

                              thresImage = blurImag.ThresholdBinary(new Gray(mThreshold), new Gray(255));
                              contours = new VectorOfVectorOfPoint();

                              Mat hierarchy = new Mat();
                              CvInvoke.FindContours(thresImage, contours, hierarchy,
                                  Emgu.CV.CvEnum.RetrType.Tree, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                              double largest = -1.0;
                              int largeIndex = -1;
                              // Find largest contour that is not close to any edge of the ROI
                              for (int i = 0; i < contours.Size; i++)
                              {
                                  double tempSize = CvInvoke.ContourArea(contours[i]);
                                  if (tempSize > largest &&
                                      ! ContourCloseToEdge(contours[i], thresImage.Width, thresImage.Height))
                                  {
                                      largest = tempSize;
                                      largeIndex = i;
                                  }

                              }*//*

                              if (largeIndex >= 0)
                              {
                                  VectorOfPoint workContour = contours[largeIndex];
                                  Moments moment = CvInvoke.Moments(workContour);

                                  pupx = moment.M10 / moment.M00;
                                  pupy = moment.M01 / moment.M00;

                                  DataRow row = new DataRow
                                  {
                                      frameID = thisFrame.FrameID,
                                      imageTime = thisFrame.ImageTime,
                                      //pupilX = pupx + mRoiLeft,
                                      //pupilY = pupy + mRoiTop,
                                      //pupilSize = CvInvoke.ContourArea(workContour),
                                      processTime = HighResolutionDateTime.UtcNow
                                  };
                                  mDataFile.WriteRecord(row);
                                  mDataFile.NextRecord();
                              }
                              else
                              {
                                  pupx = 0;
                                  pupy = 0;
                              }
                          }
                          catch
                          {

                          }*/

                        DataRow row = new DataRow
                        {
                            frameID = thisFrame.FrameID,
                            imageTime = thisFrame.ImageTime,
                            //pupilX = pupx + mRoiLeft,
                            //pupilY = pupy + mRoiTop,
                            //pupilSize = CvInvoke.ContourArea(workContour),
                            processTime = HighResolutionDateTime.UtcNow
                        };
                        mDataFile.WriteRecord(row);
                        mDataFile.NextRecord();
                        mDataFile.Flush();

                        pupx = 0;
                        pupy = 0;
                        LatencyEventArgs latency = new LatencyEventArgs();

                        double procTime = HighResolutionDateTime.UtcNow;

                        latency.Latency = procTime - thisFrame.ImageTime;
                        LatencyEvent(this, latency);


                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug("PupilProc Frame {0} at {1}", thisFrame.FrameID, procTime);
                        }
                    }
                    else
                    {
                        pupx = 0;
                        pupy = 0;
                    }

                    //  timestamp parameters
                    Rectangle TimestampBackground = new Rectangle(2, (int)mCamera.Height.Value - 25, 325, 25);
                    Gray TimestampBackgroundColor = new Gray(0);
                    System.Drawing.Point TimeStampLocation = new System.Drawing.Point(2, (int)mCamera.Height.Value - 2);
                    Gray TimeStampTextColor = new Gray(255);

                    //Create timestamp on image
                    thisFrame.Image.Draw(TimestampBackground, TimestampBackgroundColor, -1);
                    thisFrame.Image.Draw(DateTime.Now.ToString("MM/dd/yy HH:mm:ss"),
                        TimeStampLocation, Emgu.CV.CvEnum.FontFace.HersheyPlain, 2.0,
                        TimeStampTextColor, 2, Emgu.CV.CvEnum.LineType.EightConnected);

                    // add pupil center point
                    /*
                    thisFrame.Image.Draw(new CircleF(new PointF((float)pupx + mRoiLeft,
                        (float)pupy + mRoiTop), 2.0f), new Gray(255), 2);
*/
                    if (mRecord && thisFrame.FrameID % mTimelapseInterval == 0)
                    {
                        mTimelapseVid.Write(thisFrame.Image.Mat);
                        //mThreshVid.Write(thresImage.Mat);
                    }

                    if (mDisplay) //mDisplay
                    {
                        FrameData displayFrame;
                        if (mThreshImage)
                        {
                            displayFrame = new FrameData(thisFrame.FrameID,
                                thisFrame.Image, Convert.ToInt32(pupx) + mRoiLeft,
                                Convert.ToInt32(pupy) + mRoiTop);
                            /* displayFrame = new FrameData(thisFrame.FrameID,
                                 thresImage, Convert.ToInt32(pupx),
                                   Convert.ToInt32(pupy));
                            */
                        }
                        else if (mFullImage) //mFullImage
                        {
                            displayFrame = new FrameData(thisFrame.FrameID,
                                thisFrame.Image, Convert.ToInt32(pupx) + mRoiLeft,
                                Convert.ToInt32(pupy) + mRoiTop);
                        }
                        else
                        {
                            displayFrame = new FrameData(thisFrame.FrameID,
                                thisFrame.Image, Convert.ToInt32(pupx) + mRoiLeft,
                                Convert.ToInt32(pupy) + mRoiTop);
                            /*displayFrame = new FrameData(thisFrame.FrameID,
                            cropImg, Convert.ToInt32(pupx), Convert.ToInt32(pupy));
                            */
                        }
                        mDisplayQueue.Add(displayFrame);
                    }
                }
                catch(Exception e)
                {
                    logger.Error("{0} \n{1}", e.Message, e.StackTrace);
                    Thread.Sleep(3);
                }
            }
            //Do Image Display
        }

        public class DataRow
        {
            public double frameID { get; set; }
            public double imageTime { get; set; }
            //public double pupilX { get; set; }
            //public double pupilY { get; set; }
            //public double pupilSize { get; set; }
            public double processTime { get; set; }

            //public DataRow(double frameID, double imageTime, int pupilX, int pupilSize, double processTime)
            //{
            //    frameID = frameID;

            //}
        }

        public class DataRowMap : ClassMap<DataRow>
        {
            public DataRowMap()
            {
                Map(m => m.frameID).Index(0).Name("frameID");
                Map(m => m.imageTime).Index(1).Name("imageTime");
                //Map(m => m.pupilX).Index(2).Name("pupilX");
                //Map(m => m.pupilY).Index(3).Name("pupilY");
                //Map(m => m.pupilSize).Index(4).Name("pupilSize");
                Map(m => m.processTime).Index(5).Name("processTime");

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
                           /* if (mFullImage) //mFullImage
                            {
                                image.Draw(new Rectangle(mRoiLeft, mRoiTop, mRoiRight - mRoiLeft, mRoiBottom - mRoiTop), new Gray(255), 2);
                            }*/
                         /*   if (mRecord)
                            {
                                image.Draw(new CircleF(new PointF(dispFrame.X, dispFrame.Y), 2.0f),
                                    new Gray(mThreshImage ? 125 : 255), 2);
                            }*/
                            mComponent.HandleDisplayImage(image);

                            mDataFile.Flush();

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


        public void ROIChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mRoiTop = page.mTopTrackBar.mTrackbar.Value;
            mRoiBottom = page.mBottomTrackBar.mTrackbar.Value;
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
                RigSelection rigSelector = new RigSelection();
                DialogResult result = rigSelector.ShowDialog();
                if (result == DialogResult.OK)
                {
                    mRunning = true;
                    string rigLabel = rigSelector.SelectedRig;
                    int cohortNumber = int.Parse(rigSelector.CohortNumber);

                    Regex re = new Regex(@"([a-zA-Z]+)(\d+)");
                    Match rigMatch = re.Match(rigLabel);

                    int rigNumber = int.Parse(rigMatch.Groups[2].Value);

                    string vidPath = Path.Combine(ConfigurationManager.AppSettings["output_path_root"], rigLabel,
                        "r" + rigNumber.ToString("D2") + "c" + cohortNumber.ToString("D3"));

                    page.mRigNum.Text = rigNumber.ToString("D2");
                    page.mCohortNum.Text = cohortNumber.ToString("D3");

                    //Check that output directory exists
                    if (!Directory.Exists(vidPath))
                    {
                        Directory.CreateDirectory(vidPath);
                        logger.Info($"Output directory did not exist. Creating output directory {vidPath}");
                    }
                    else
                    {
                        logger.Info($"Output directory: {vidPath}");
                    }

                    // create csv file 
                    string csvFileName = (vidPath + Path.DirectorySeparatorChar + "searchbehavior_" 
                            + serialNumber + DateTime.Now.ToString("_yyyy_MM_dd_hh_mm_ss") + ".csv");

                    writer = new StreamWriter(csvFileName);
                    mDataFile = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);
                    mDataFile.Configuration.RegisterClassMap<DataRowMap>();
                    mDataFile.WriteHeader<DataRow>();
                    mDataFile.NextRecord();

                    // Create video file paths
                    string timelapseFilename = (vidPath + Path.DirectorySeparatorChar + "timelapse_" 
                        + serialNumber + DateTime.Now.ToString("_yyyy_MM_dd_hh_mm_ss"));

                    // Create Video Writers
                    mTimelapseVid = new ImprovedVideoWriter(timelapseFilename, ImprovedVideoWriter.VideoCompressionType.H264, 50.0, (int)mCamera.Width.Value, (int)mCamera.Height.Value, true);
                    // mTimelapseVid.MaxFileSize = 3800;
                    // mTimelapseVid.MaxFileSize = 16000;
                    mTimelapseVid.MaxFileSize = 50000;
                    
                    //mThreshVid = new ImprovedVideoWriter(threshVidFilename, ImprovedVideoWriter.VideoCompressionType.H264,)
                    logger.Info("Pupil recodings stared on camera {5} with the following TOP, BOTTOM, LEFT, RIGHT, THRESHOLD values: {0}, {1}, {2}, {3}, {4}",
                         mRoiTop, mRoiBottom, mRoiLeft, mRoiRight, mThreshold, mCamera.DeviceSerialNumber.ToString());

                }
                else  // they pushed cancel or similar
                {
                    page.mRecord.Checked = false;
                }
            }
            else
            {
                if (mRunning)
                {
                    mTimelapseVid.Close();
                    mTimelapseVid = null;
                    mRunning = false;
                    page.mCohortNum.Text = "";
                    page.mRigNum.Text = "";
                    mDataFile.Flush();

                }
            }
        }

        public void FullImageChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mFullImage = page.mFullImage.Checked;
        }

        public void ThreshImageChangeHandler(object sender, System.EventArgs e)
        {
            CameraTabPage page = (CameraTabPage)sender;
            mThreshImage = page.mThreshImage.Checked;
        }

        public void DisplayChangeHandler(object sender, System.EventArgs e)
        {

        }

        public void Close()
        {
            mStillAlive = false;
            Thread.Sleep(100);
            mDataFile.Flush();
            if (mRunning)
            {
                mTimelapseVid.Close();
            }
            if (mGrabThread.IsAlive)
            {
                mGrabThread.Abort();
            }
            if (mPupilProcessThread.IsAlive)
            {
                mPupilProcessThread.Abort();
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
        public int X;
        public int Y;
        public FrameData() { }

        public FrameData(long frameId, Image<Gray, Byte> image, int x = 0, int y=0)
        {
            this.FrameID = frameId;
            this.Image = image;
            X = x;
            Y = y;

        }


    }

}
