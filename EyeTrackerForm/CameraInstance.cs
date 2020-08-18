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
        private int counter = 0;
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

        public bool ContourCloseToEdge(VectorOfPoint contour, int imageWidth, int imageHeight,
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
        }

        public void DoPupilTracking()
        {
            
            double pupx;
            double pupy;
            FrameData thisFrame;

            Image<Gray, Byte> thresImage;
            Image<Gray, Byte> blurImag;
            Image<Gray, Byte> image;
            VectorOfVectorOfPoint contours;
            Image<Gray, Byte> cropImg;

            while (mStillAlive)
            {
                try
                {
                    thisFrame = mPupilQueue.Take();
                    logger.Debug("size of opupil queue is {0}", mPupilQueue.Count);
                    image = thisFrame.Image;
                    thresImage = image;
                    cropImg = image.Copy(new Rectangle(mRoiLeft, mRoiTop, mRoiRight - mRoiLeft, mRoiBottom - mRoiTop));
                    if (mRecord)
                    {

                        try
                        {

                            blurImag = cropImg.SmoothBlur(5, 5);
                            //blurImag =

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

                            }

                            if (largeIndex >= 0)
                            {
                                VectorOfPoint workContour = contours[largeIndex];

                                Moments moment = CvInvoke.Moments(workContour);


                                pupx = moment.M10 / moment.M00;
                                pupy = moment.M01 / moment.M00;
                            }
                            else
                            {
                                pupx = 0;
                                pupy = 0;
                            }


                        }
                        catch
                        {
                            pupx = 0;
                            pupy = 0;
                        }
                        // Calculate mcc outputs in range 0 to 10 V from edge to edge of the ROI
                        float xval = (float)(pupx / (mRoiRight - mRoiLeft)* 10.0);
                        float yval = (float)(pupy / (mRoiBottom - mRoiTop) * 10.0);

                        LatencyEventArgs latency = new LatencyEventArgs();

                        double procTime = HighResolutionDateTime.UtcNow;

                        latency.Latency = procTime - thisFrame.ImageTime;
                        LatencyEvent(this, latency);

                        if (logger.IsDebugEnabled)
                        {
                            logger.Debug("PupilProc Frame {0} at {1}", thisFrame.FameID, procTime);
                        }
                    }
                    else
                    {
                        pupx = 0;
                        pupy = 0;

                    }

                    if (mDisplay) //mDisplay
                    {
                        FrameData displayFrame;
                        if (mThreshImage)
                        {
                            displayFrame = new FrameData(thisFrame.FameID, thresImage, Convert.ToInt32(pupx), Convert.ToInt32(pupy));
                        }
                        else if (mFullImage) //mFullImage
                        {
                            displayFrame = new FrameData(thisFrame.FameID, thisFrame.Image, Convert.ToInt32(pupx) + mRoiLeft, Convert.ToInt32(pupy) + mRoiTop);
                        }
                        else
                        {
                            displayFrame = new FrameData(thisFrame.FameID, cropImg, Convert.ToInt32(pupx), Convert.ToInt32(pupy));
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



                            if (mFullImage) //mFullImage
                            {
                                image.Draw(new Rectangle(mRoiLeft, mRoiTop, mRoiRight - mRoiLeft, mRoiBottom - mRoiTop), new Gray(255), 2);
                            }
                            if (mRecord)
                            {
                                image.Draw(new CircleF(new PointF(dispFrame.X, dispFrame.Y), 2.0f),
                                    new Gray(mThreshImage ? 125 : 255), 2);
                            }
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
                            int thiskldjf = 1;
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
                logger.Info("Pupil recodings stared on camera {5} with the following TOP, BOTTOM, LEFT, RIGHT, THRESHOLD values: {0}, {1}, {2}, {3}, {4}",
                    mRoiTop, mRoiBottom, mRoiLeft, mRoiRight, mThreshold, mCamera.DeviceSerialNumber.ToString());
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
        public long FameID { get; set; }
        public Image<Gray, Byte> Image { get; set; }
        public double ImageTime { get; set; }
        public int X;
        public int Y;
        public FrameData() { }

        public FrameData(long frameId, Image<Gray, Byte> image, int x = 0, int y=0)
        {
            this.FameID = frameId;
            this.Image = image;
            X = x;
            Y = y;
            
        }

        
    }

}
