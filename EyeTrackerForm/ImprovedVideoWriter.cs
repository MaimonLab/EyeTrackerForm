using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace EyeTrackerForm
{
    /// <summary>
    /// Wrapper around openCV VideoWriter for additional functionality
    /// </summary>
    class ImprovedVideoWriter
    {
        /// <summary>
        /// Max file size limit in MB
        /// </summary>
        public int MaxFileSize { get; set; }


        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        VideoWriter mVideoWriter;
        string mBaseFilePath;
        int mFileCount = 0;

        int mBackEndAPI;
        int mFCC;
        double mFPS;
        System.Drawing.Size mFrameSize;
        bool mIsColor;
        string mFileExt;

        /// <summary>
        /// Types of Video Compression
        /// </summary>
        public enum VideoCompressionType
        {
            Uncompressed,
            //Mjpg,
            H264
        }

        //Constructor
        public ImprovedVideoWriter(string fileName, VideoCompressionType type, double fps, int width, int height, bool isColor=false)
        {
            mFileExt = "mp4";
            MaxFileSize = 0;

            // Get File Path
            mBaseFilePath = Path.GetFullPath(fileName);
            string firstFile = BuildFileName(mFileCount, mFileExt);

            // Set Member Variables
            mFPS = fps;
            mFrameSize = new System.Drawing.Size(width, height);
            mIsColor = isColor;


            // Get Backend API
            Backend[] backends = CvInvoke.WriterBackends;
            int backend_idx = 0; //any backend;
            foreach (Backend be in backends)
            {
                if (be.Name.Equals("MSMF"))
                {
                    backend_idx = be.ID;
                    break;
                }
            }
            mBackEndAPI = backend_idx;

            //Get Compression type
            // TODO: Add more Compression types and default
            int fcc = 0;
            switch (type)
            {
                case VideoCompressionType.H264:
                    fcc = VideoWriter.Fourcc('H', '2', '6', '4');
                    break;

            }
            fcc = VideoWriter.Fourcc('H', '2', '6', '4');
            mFCC = fcc;

            //Create Video Writer
            mVideoWriter = new VideoWriter(firstFile, mBackEndAPI, mFCC, mFPS, mFrameSize, mIsColor);

            
        }
        public void Close()
        {
            mVideoWriter.Dispose();
        }

        //Destructor
        //~ImprovedVideoWriter()
        //{ 
        //    mVideoWriter.Dispose();
        //}

        /// <summary>
        /// Writes Image to the video file.
        /// </summary>
        /// <param name="image">Image to be written to video file</param>
        public void Write(Mat image)
        {
            // If file is past max file size, create new file
            if(IsFilePastMax())
            {
                NextWriter();
            }

            // write image
            mVideoWriter.Write(image);
            
        }

        /// <summary>
        /// Gets the full file path for video file using the base name
        /// </summary>
        /// <param name="count">File count of the current writer</param>
        /// <param name="fileExt">File extention to be applied to file</param>
        /// <returns>Returns full path to video writer location</returns>
        private string BuildFileName(int count, string fileExt)
        {
            return String.Format("{0}-{1:000}.{2}", mBaseFilePath, count, fileExt);
        }

        /// <summary>
        /// Checks if file is past the maximum file size limit
        /// </summary>
        /// <returns>Returns True if file is past max size limit</returns>
        private bool IsFilePastMax()
        {
            bool retVal = false;

            // Only check it Maxfile size limit is set
            if (MaxFileSize > 0)
            {
                // Check size of file
                FileInfo file = new FileInfo(BuildFileName(mFileCount, mFileExt));
                if (file.Length/1000000 > MaxFileSize)
                {
                    retVal = true;
                }
            }
            return retVal;

        }

        /// <summary>
        /// Disposes of current video writer and creates the next one
        /// </summary>
        private void NextWriter()
        {
            mVideoWriter.Dispose();
            mFileCount++;
            string fileName = BuildFileName(mFileCount, mFileExt);
            mVideoWriter = new VideoWriter(fileName, mBackEndAPI, mFCC, mFPS, mFrameSize, mIsColor);
        }
        



    }
}
