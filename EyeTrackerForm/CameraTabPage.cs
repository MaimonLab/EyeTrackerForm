﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EyeTrackerForm
{
    public class CameraTabPage : TabPage
    {
        public int DEFAULTTOPVALUE = 10;
        public int DEFAULTBOTTOMVALUE = 20;
        public int DEFAULTLEFTVALUE = 10;
        public int DEFAULTRIGHTVALUE = 20;
        


        public CameraSlider mTopTrackBar;
        public CameraSlider mBottomTrackBar;
        public CameraSlider mLeftTrackBar;
        public CameraSlider mRightTrackBar;
        public CameraSlider mThresholdTrackBar;
        public CheckBox mRecord;
        public CheckBox mFullImage;
        public Label mLatencyLabel;
        public Label mLatencyBox;


        public CameraTabPage() : base() { }
        public CameraTabPage(string s) : base(s) { }
        public event EventHandler ROIChanged;
        public event EventHandler RecordChange;
        public event EventHandler FullImageChange;

        public void Init ()
        {
            mTopTrackBar = MakeTrackBar(CameraSliderType.TOP);
            mTopTrackBar.SetMax(1023);
            mTopTrackBar.SetValue(DEFAULTTOPVALUE);
            

            mBottomTrackBar = MakeTrackBar(CameraSliderType.BOTTOM);
            mBottomTrackBar.SetMax(1023);
            mBottomTrackBar.SetValue(DEFAULTBOTTOMVALUE);
            

            mLeftTrackBar = MakeTrackBar(CameraSliderType.LEFT);
            mLeftTrackBar.SetMax(1279);
            mLeftTrackBar.SetValue(DEFAULTLEFTVALUE);
            

            mRightTrackBar = MakeTrackBar(CameraSliderType.RIGHT);
            mRightTrackBar.SetMax(1279);
            mRightTrackBar.SetValue(DEFAULTRIGHTVALUE);

            mThresholdTrackBar = MakeTrackBar(CameraSliderType.THRESHOLD);
            mThresholdTrackBar.SetMax(255);
            mThresholdTrackBar.SetValue(200);
            
            mTopTrackBar.SliderChange += this.TrackChangeHandler;
            mBottomTrackBar.SliderChange += this.TrackChangeHandler;
            mLeftTrackBar.SliderChange += this.TrackChangeHandler;
            mRightTrackBar.SliderChange += this.TrackChangeHandler;
            mThresholdTrackBar.SliderChange += this.TrackChangeHandler;


            // Check Boxes
            mRecord = new CheckBox();
            mRecord.AutoSize = true;
            mRecord.Location = new System.Drawing.Point(10, 360);
            mRecord.TabIndex = 4;
            mRecord.Size = new System.Drawing.Size(80, 17);
            mRecord.Name = "recordCheckBox";
            mRecord.Text = "Record";
            mRecord.UseVisualStyleBackColor = true;

            mFullImage = new CheckBox();
            mFullImage.AutoSize = true;
            mFullImage.Location = new System.Drawing.Point(10, 387);
            mFullImage.TabIndex = 5;
            mFullImage.Size = new System.Drawing.Size(80, 17);
            mFullImage.Name = "FullImageCheckBox";
            mFullImage.Text = "Full Image";
            mFullImage.UseVisualStyleBackColor = true;
            mFullImage.Checked = true;

            
            mRecord.CheckStateChanged += this.RecordChangeHandler;
            mFullImage.CheckStateChanged += this.FullImageChangeHandler;

            // Latency Box

            mLatencyLabel = new Label();
            mLatencyLabel.AutoSize = true;
            mLatencyLabel.Location = new System.Drawing.Point(10, 420);
            mLatencyLabel.Name = "latency";
            mLatencyLabel.Size = new System.Drawing.Size(35, 13);
            mLatencyLabel.TabIndex = 6;
            mLatencyLabel.Text = "Latency";

            mLatencyBox = new Label();
            mLatencyBox.AutoSize = true;
            mLatencyBox.Location = new System.Drawing.Point(55, 420);
            mLatencyBox.Name = "latencybox";
            mLatencyBox.Size = new System.Drawing.Size(35, 13);
            mLatencyBox.TabIndex = 7;
            mLatencyBox.Text = "0";






            this.Controls.Add(mTopTrackBar);
            this.Controls.Add(mBottomTrackBar);
            this.Controls.Add(mLeftTrackBar);
            this.Controls.Add(mRightTrackBar);
            this.Controls.Add(mThresholdTrackBar);
            this.Controls.Add(mRecord);
            this.Controls.Add(mFullImage);
            this.Controls.Add(mLatencyLabel);
            this.Controls.Add(mLatencyBox);

        }

        private CameraSlider MakeTrackBar(CameraSliderType pos)
        {
            CameraSlider newSlider = new CameraSlider(pos);
            newSlider.Location = new System.Drawing.Point(10, 10 + (int)pos* 65);
            newSlider.TabIndex = (int)pos;

            return newSlider;
        }


        public void TrackChangeHandler(object sender, System.EventArgs e)
        {
            ROIChanged(this, new EventArgs());
        }

        public void RecordChangeHandler(object sender, System.EventArgs e)
        {
            RecordChange(this, new EventArgs());
        }

        public void FullImageChangeHandler(object sender, System.EventArgs e)
        {
            FullImageChange(this, new EventArgs());
        }

        public void HandleLatencyEvent(object sender, LatencyEventArgs e)
        {
            if (this.mLatencyBox.InvokeRequired)
            {
                this.mLatencyBox.BeginInvoke((MethodInvoker)delegate () { this.mLatencyBox.Text = e.Latency.ToString(); ; });
            }
            else
            {
                mLatencyBox.Text = e.Latency.ToString();
            }
        }


    }


    public  class CameraSlider : Panel
    {

        public int SLIDERWIDTH = 150;
        public int SLIDERHEIGHT = 45;
        public int SLIDERINITX = 0;
        public int SLIDERINITY = 15;

        public TrackBar mTrackbar;
        public Label mLabel;
        public Label mValue;
        public CameraSliderType mType;
        public event EventHandler SliderChange;

        public CameraSlider(CameraSliderType label) : base()
        {
            mType = label;

            mLabel = new Label();
            mLabel.AutoSize = true;
            mLabel.Location = new System.Drawing.Point(0, 0);
            mLabel.Name = "label1";
            mLabel.Size = new System.Drawing.Size(35, 13);
            mLabel.TabIndex = 0;
            string name;
            switch (label)
            {
                case CameraSliderType.TOP:
                    name = "Top";
                    break;
                case CameraSliderType.BOTTOM:
                    name = "Bottom";
                    break;
                case CameraSliderType.LEFT:
                    name = "Left";
                    break;
                case CameraSliderType.RIGHT:
                    name = "Right";
                    break;
                case CameraSliderType.THRESHOLD:
                    name = "Threshold";
                    break;
                default:
                    name = "";
                    break;
            }
            mLabel.Text = name;


            mTrackbar = new TrackBar();
            mTrackbar.Location = new System.Drawing.Point(SLIDERINITX, SLIDERINITY);
            mTrackbar.Name = "bar" ;
            mTrackbar.Size = new System.Drawing.Size(SLIDERWIDTH, SLIDERHEIGHT);
            mTrackbar.TabIndex = 1;

            mValue = new Label();
            mValue.AutoSize = true;
            mValue.Location = new System.Drawing.Point(SLIDERWIDTH+10, SLIDERINITY);
            mValue.Name = "label2";
            mValue.Size = new System.Drawing.Size(35, 13);
            mValue.TabIndex = 2;
            mValue.Text = "";

            this.Size = new System.Drawing.Size(SLIDERWIDTH+40, SLIDERHEIGHT+20);


            mTrackbar.ValueChanged += this.TrackChangeHandler;

            this.Controls.Add(mLabel);
            this.Controls.Add(mTrackbar);
            this.Controls.Add(mValue);


        }

        public void TrackChangeHandler(object sender, System.EventArgs e)
        {
            TrackBar thisSender = (TrackBar)sender;
            mValue.Text = thisSender.Value.ToString();
            if (SliderChange != null)
            {
                SliderChange(this, new EventArgs());
            }
        }

        public void SetValue(int value)
        {
            mTrackbar.Value = value;
        }

        public void SetMax(int value)
        {
            mTrackbar.Maximum = value;
        }

       
    }

    

    public enum CameraSliderType
    {
        TOP,
        BOTTOM,
        LEFT,
        RIGHT,
        THRESHOLD
    }

    public class LatencyEventArgs : EventArgs
    {
        public double Latency { get; set; }
    }


}
