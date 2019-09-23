using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpinnakerNET.GUI;

namespace EyeTrackerForm
{
    public partial class Form1 : Form
    {
        private GUIFactory mFactory;
        private EyeTrackerController mController;
        public Form1(EyeTrackerController controller)
        {
            InitializeComponent();
            mFactory = new GUIFactory();
            mController = controller;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void addCameraToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CameraSelectionWindow window = mFactory.GetCameraSelectionWindow();
            window.Height = 450.0;
            SpinnakerNET.GUI.CameraSelectionWindow.DeviceEventArgs retval = window.ShowModal(false);
            if (retval != null)
            {
                mController.AddCamera(retval.Camera);
            }
        }



        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void connectMCCDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mController.AddMCC();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mController.Close();
        }

        private void saveCameraConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CameraTabPage thisPage = (CameraTabPage)this.tabControl1.SelectedTab;
            System.IO.File.WriteAllLines(
                System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, thisPage.Text + ".config"),
                new string[]{
                    "Timelapse Interval: " + thisPage.mTimelapseTrackBar.mTrackbar.Value.ToString(),
                    "Display Interval:" + thisPage.mDisplayIntervalTrackBar.mTrackbar.Value.ToString(),
                    "Feeding Video Length: " + thisPage.mFeedVidLengthTrackBar.mTrackbar.Value.ToString()
                });
        }
    }
}
