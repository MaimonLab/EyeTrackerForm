﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpinnakerNET;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Emgu.CV;
using MccDaq;
using System.IO;

using Emgu.CV.Structure;


namespace EyeTrackerForm
{

    public class EyeTrackerController
    {
        static readonly object _object = new object();
        public MccDaq.MccBoard mDaqBoard;
        public bool FirstCam = true;
        public bool MccInit = false;



        CameraComponent mCameraComponent;
        Form1 mForm;
        bool wasMinimized = false;
        string mActiveTab ="";
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

        public void AddMCC()
        {
            try
            {
                //VOutOptions Options = VOutOptions.Default;
                //MccDaq.ErrorInfo ULStat;
                MccDaq.Range thisRange;
                MccDaq.DaqDeviceManager.IgnoreInstaCal();
                MccDaq.DaqDeviceDescriptor[] inventory = MccDaq.DaqDeviceManager.GetDaqDeviceInventory(MccDaq.DaqDeviceInterface.Any);
                int numDevDiscovered = inventory.Length;
                if (numDevDiscovered > 0)
                {
                    mDaqBoard = MccDaq.DaqDeviceManager.CreateDaqDevice(0, inventory[0]);
                }
                mDaqBoard.BoardConfig.GetRange(out thisRange);
                //ULStat = mDaqBoard.VOut(0, MccDaq.Range.Uni10Volts, 2,Options);
                mForm.toolStripStatusLabel1.Text = "MCC Device Connected.";
                MccInit = true;



            }
            catch (ULException ule)
            {
                mForm.toolStripStatusLabel1.Text = "MCC Device Failed.";
                MccInit = false;
            }


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

        public void FireMcc ( int chan1, int chan2, float val1, float val2)
        {
            if (MccInit)
            {
                VOutOptions Options = VOutOptions.Default;

                mDaqBoard.VOut(chan1, MccDaq.Range.Uni10Volts, val1, Options);
                mDaqBoard.VOut(chan2, MccDaq.Range.Uni10Volts, val2, Options);
            }
        }

        public void Close()
        {
            if (MccInit)
            {
                MccInit = false;
                DaqDeviceManager.ReleaseDaqDevice(mDaqBoard);
            }

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
