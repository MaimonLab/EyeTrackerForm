using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpinnakerNET;
using System.Drawing;
using Emgu.CV;

using Emgu.CV.Structure;

namespace EyeTrackerForm
{
    public class CameraComponent
    {
        public List<CameraInstance> mCameraList;
        EyeTrackerController mController;



        public CameraComponent(EyeTrackerController controller)
        {
            mCameraList = new List<CameraInstance>();
            mController = controller;
        }

        public CameraInstance AddCameraInstance(IManagedCamera camera)
        {
            CameraInstance newCamera = new CameraInstance(camera, this);
            mCameraList.Add(newCamera);
            return newCamera;
        }

        public void HandleDisplayImage(Emgu.CV.Image<Gray, Byte> image)
        {
            mController.HandleDisplayImage(image);
        }
        public void Close()
        {
            foreach (CameraInstance cam in mCameraList)
            {
                cam.Close();
                
            }
        }
    }
}
