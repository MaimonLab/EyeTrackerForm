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
        static readonly object _object = new object();

        public bool FirstCam = true;


        CameraComponent mCameraComponent;
        Form1 mForm;
        bool wasMinimized = false;
        string mActiveTab ="";


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

            string serialNumber = camera.DeviceSerialNumber.ToString();
            CameraTabPage newTab = new CameraTabPage(serialNumber);
            newTab.Init();
            newTab.ROIChanged += cam.ROIChangeHandler;
            newTab.RecordChange += cam.RecordChangeHandler;
            newTab.FullImageChange += cam.FullImageChangeHandler;
            //cam.LatencyEvent += newTab.HandleLatencyEvent;
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

            string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, serialNumber + ".config");
            if (File.Exists(config))
            {
                string[] lines = File.ReadAllLines(config);
                newTab.mTimelapseTrackBar.mTrackbar.Value = Int32.Parse(lines[0].Split(' ')[1]);
                newTab.mDisplayIntervalTrackBar.mTrackbar.Value = Int32.Parse(lines[0].Split(' ')[1]);
                newTab.mFeedVidLengthTrackBar.mTrackbar.Value = Int32.Parse(lines[1].Split(' ')[1]);

            }



            mForm.tabControl1.TabPages.Add(newTab);
            mActiveTab = serialNumber;


        }


        public void HandleTabChange (object sender, EventArgs e)
        {
            TabControl thisControl = (TabControl)sender;
            ActivateTabDisplay(thisControl.SelectedTab.Text);
            mActiveTab = thisControl.SelectedTab.Text;
        }

        public void HandleDisplayImage(Emgu.CV.Image<Gray, Byte> image)
        {

            float scaleHeight = (float)mForm.imageBox1.Height / (float)image.Height;
            float scaleWidth = (float)mForm.imageBox1.Width / (float)image.Width;
            float scale = Math.Min(scaleHeight, scaleWidth);

            mForm.imageBox1.Image = image.Resize((int)(image.Width*scale), (int)(image.Height * scale), Emgu.CV.CvEnum.Inter.Linear);
            //Thread.Sleep(2);



        }


        public void Close()
        {
            mCameraComponent.Close();
        }

        public void HandleFormResize(FormWindowState state)
        {
            if (state == FormWindowState.Minimized)
            {
                ActivateTabDisplay("null");
                wasMinimized = true;
            }
            else
            {
                if (wasMinimized)
                {
                    wasMinimized = false;
                    ActivateTabDisplay(mActiveTab);

                }
            }
        }

        private void ActivateTabDisplay(string tabSerialNumber)
        {
            foreach (CameraInstance item in mCameraComponent.mCameraList)
            {
                if (item.serialNumber == tabSerialNumber)
                {
                    item.mDisplay = true;

                }
                else
                {
                    item.mDisplay = false;
                }
            }

        }


    }
}
