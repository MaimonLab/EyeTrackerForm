using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.Threading;



namespace EyeTrackerForm
{
    public class ImageEventListener : ManagedImageEvent
    {
        
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private string deviceSerialNumber;
        public const int NumImages = 20;
        public int imageCnt;
        public CameraInstance mInstance;
        public IManagedCamera mCam;
        public Queue<ManagedImage> mImageQueue;

        public event EventHandler<NewImageEventArgs> NewImageEvent;
        // The constructor retrieves the serial number and initializes the
        // image counter to 0.
        public ImageEventListener(IManagedCamera cam, CameraInstance instance, Queue<ManagedImage> imageQueue    )
        {
            // Initialize image counter to 0
            mImageQueue = imageQueue;
            mCam = cam;
            imageCnt = 0;
            mInstance = instance;
            // Retrieve device 
            INodeMap nodeMap = cam.GetTLDeviceNodeMap();
            deviceSerialNumber = "";
            IString iDeviceSerialNumber = nodeMap.GetNode<IString>("DeviceSerialNumber");
            if (iDeviceSerialNumber != null && iDeviceSerialNumber.IsReadable)
            {
                deviceSerialNumber = iDeviceSerialNumber.Value;
            }
        }


        override protected void OnImageEvent(ManagedImage image)
        {
            if (image.IsIncomplete)
            {
                Console.WriteLine("Image incomplete with image status {0}...\n", image.ImageStatus);
            }
            else
            {

                logger.Debug("Got image {0} at time {1}", image.ChunkData.CounterValue, HighResolutionDateTime.UtcNow);
                ManagedImage workImage = new ManagedImage();
                workImage.DeepCopy(image);
                image.Release();
                //image.Dispose();
                mImageQueue.Enqueue(workImage);
                //mInstance.HandleImageEvent(workImage);


                //Thread displayThread = new Thread(this.SignalNewImage);
                //displayThread.Start(workImage);

                //NewImageEventArgs args = new NewImageEventArgs();
                //args.image = workImage;
                //NewImageEvent(this, args);
                //mInstance.HandleImageEvent(workImage);
                //workImage.Dispose();
                
            }
        }

        public void SignalNewImage(object image)

        {
            ManagedImage thisImage = (ManagedImage)image;
            NewImageEventArgs args = new NewImageEventArgs();
            args.image = thisImage;
            NewImageEvent(this, args);
        }
    }

    

    public class NewImageEventArgs : EventArgs
    {
        public ManagedImage image { get; set; }
    }
}
