using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpinnakerNET;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Emgu.CV;
using System.IO;

using Emgu.CV.Structure;


namespace EyeTrackerForm
{

    public class EyeTrackerController
    {
        bool isLocked = false;
        static readonly object _object = new object();
        public bool FirstCam = true;
        public bool MccInit = false;



        CameraComponent mCameraComponent;
        Form1 mForm;
        // MccInstance

        public EyeTrackerController()
        {
            mCameraComponent = new CameraComponent(this);

        }
        public void AddForm(Form1 form)
        {
            mForm = form;
            mForm.tabControl1.SelectedIndexChanged += HandleTabChange;
           
        }

        public void AddCamera(IManagedCamera camera)
        {
            CameraInstance cam = mCameraComponent.AddCameraInstance(camera);
            
            string serailNumber = camera.DeviceSerialNumber.ToString();
            CameraTabPage newTab = new CameraTabPage(serailNumber);
            newTab.Init();
            newTab.ROIChanged += cam.ROIChangeHandler;
            newTab.RecordChange += cam.RecordChangeHandler;
            newTab.FullImageChange += cam.FullImageChangeHandler;
            newTab.ThreshImageChange += cam.ThreshImageChangeHandler;
            cam.LatencyEvent += newTab.HandleLatencyEvent;
            cam.FrameRateEvent += newTab.HandleFrameRateEvent;
            if (FirstCam)
            {
                cam.Xchannel = 0;
                cam.Ychannel = 1;
                cam.mDisplay = true;
                FirstCam = false;
            }
            else
            {
                cam.Xchannel = 2;
                cam.Ychannel = 3;
            }

            string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, serailNumber + ".config");
            if (File.Exists(config))
            {
                string[] lines = File.ReadAllLines(config);
                newTab.mTopTrackBar.mTrackbar.Value = Int32.Parse(lines[0].Split(' ')[1]);
                newTab.mBottomTrackBar.mTrackbar.Value = Int32.Parse(lines[1].Split(' ')[1]);
                newTab.mLeftTrackBar.mTrackbar.Value = Int32.Parse(lines[2].Split(' ')[1]);
                newTab.mRightTrackBar.mTrackbar.Value = Int32.Parse(lines[3].Split(' ')[1]);
                newTab.mThresholdTrackBar.mTrackbar.Value = Int32.Parse(lines[4].Split(' ')[1]);
            }



            mForm.tabControl1.TabPages.Add(newTab);


        }

      

        public void HandleTabChange (object sender, EventArgs e)
        {
            TabControl thisControl = (TabControl)sender;
            foreach (CameraInstance item in mCameraComponent.mCameraList)
            {
                if (item.serialNumber == thisControl.SelectedTab.Text)
                {
                    item.mDisplay = true;

                }
                else
                {
                    item.mDisplay = false;
                }
            }
        }

        public void HandleDisplayImage(Emgu.CV.Image<Gray, Byte> image)
        {
            
            if (!isLocked)
            {
                isLocked = true;
                float scaleHeight = (float)mForm.imageBox1.Height / (float)image.Height;
                float scaleWidth = (float)mForm.imageBox1.Width / (float)image.Width;
                float scale = Math.Min(scaleHeight, scaleWidth);

                mForm.imageBox1.Image = image.Resize((int)(image.Width*scale), (int)(image.Height * scale), Emgu.CV.CvEnum.Inter.Linear);
                //Thread.Sleep(2);
                isLocked = false;
            }

            
            
        }


        public void Close()
        {
             mCameraComponent.Close();
        }


    }
}
