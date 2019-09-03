using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using SpinnakerNET.GUI;

namespace EyeTrackerForm
{
    static class Program
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {




            //GUIFactory mFactory = new GUIFactory();
            //CameraSelectionWindow window = mFactory.GetCameraSelectionWindow();
            //window.Height = 450.0;
            //SpinnakerNET.GUI.CameraSelectionWindow.DeviceEventArgs retval = window.ShowModal(false);
            //try
            //{
            //    if (retval != null)
            //    {
            //        retval.Camera.Init();
            //        Thread.Sleep(2500);
            //        INodeMap nodeMap = retval.Camera.GetNodeMap();
            //        retval.Camera.TriggerSource.Value = TriggerSourceEnums.Line2.ToString();
            //        retval.Camera.TriggerActivation.Value = TriggerActivationEnums.RisingEdge.ToString();
            //        retval.Camera.TriggerOverlap.Value = TriggerOverlapEnums.ReadOut.ToString();
            //        retval.Camera.ExposureMode.Value = ExposureModeEnums.Timed.ToString();
            //        retval.Camera.ExposureAuto.Value = ExposureAutoEnums.Off.ToString();
            //        IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
            //        iExposureTime.Value = 19755.0;
            //        retval.Camera.TriggerMode.Value = TriggerModeEnums.On.ToString();
            //        Console.WriteLine("camera updated");

            //    }
            //    else
            //    {
            //        Console.WriteLine("no camera selected");

            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("error configuring camera");
            //}


            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                EyeTrackerController controller = new EyeTrackerController();
                Form1 thisForm = new Form1(controller);
                controller.AddForm(thisForm);
                //throw new Exception();
                Application.Run(thisForm);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "/n" + e.StackTrace, "Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
    }
}
