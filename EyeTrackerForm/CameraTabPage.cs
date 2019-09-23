using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EyeTrackerForm
{
    public class CameraTabPage : TabPage
    {
        public int DEFAULTTIMELAPSEVALUE = 30;
        public int DEFAULTFEEDVIDLENGTHVALUE = 10;
        public int DEFAULTDISPLAYINTERVAL = 10;
        public int DEFAULTLEFTVALUE = 10;
        public int DEFAULTRIGHTVALUE = 20;



        public CameraSlider mTimelapseTrackBar;
        public CameraSlider mDisplayIntervalTrackBar;
        public CameraSlider mFeedVidLengthTrackBar;

        public CheckBox mRecord;
        public CheckBox mFullImage;
        public Label mPathToWatchLabel;
        public Label mPathToWatchBox;


        public CameraTabPage() : base() { }
        public CameraTabPage(string s) : base(s) { }
        public event EventHandler ROIChanged;
        public event EventHandler RecordChange;
        public event EventHandler FullImageChange;

        public void Init()
        {
            mTimelapseTrackBar = MakeTrackBar(CameraSliderType.timelapse);
            mTimelapseTrackBar.SetMax(180);
            mTimelapseTrackBar.SetValue(DEFAULTTIMELAPSEVALUE);


            mFeedVidLengthTrackBar = MakeTrackBar(CameraSliderType.FeedVidLength);
            mFeedVidLengthTrackBar.SetMax(90);
            mFeedVidLengthTrackBar.SetValue(DEFAULTFEEDVIDLENGTHVALUE);

            mDisplayIntervalTrackBar = MakeTrackBar(CameraSliderType.DisplayInterval);
            mDisplayIntervalTrackBar.SetMax(30);
            mDisplayIntervalTrackBar.SetValue(DEFAULTDISPLAYINTERVAL);



            mTimelapseTrackBar.SliderChange += this.TrackChangeHandler;
            mFeedVidLengthTrackBar.SliderChange += this.TrackChangeHandler;
            mDisplayIntervalTrackBar.SliderChange += this.TrackChangeHandler;


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

            // PathToWatch Box

            mPathToWatchLabel = new Label();
            mPathToWatchLabel.AutoSize = true;
            mPathToWatchLabel.Location = new System.Drawing.Point(10, 420);
            mPathToWatchLabel.Name = "PathToWatch";
            mPathToWatchLabel.Size = new System.Drawing.Size(35, 13);
            mPathToWatchLabel.TabIndex = 6;
            mPathToWatchLabel.Text = "PathToWatch:";

            mPathToWatchBox = new Label();
            mPathToWatchBox.AutoSize = true;
            mPathToWatchBox.Location = new System.Drawing.Point(10, 445);
            mPathToWatchBox.Name = "PathToWatchbox";
            mPathToWatchBox.Size = new System.Drawing.Size(35, 13);
            mPathToWatchBox.MaximumSize = new System.Drawing.Size(190, 0);
            mPathToWatchBox.AutoSize = true;
            mPathToWatchBox.TabIndex = 7;
            mPathToWatchBox.Text = "None";






            this.Controls.Add(mTimelapseTrackBar);
            this.Controls.Add(mDisplayIntervalTrackBar);
            this.Controls.Add(mFeedVidLengthTrackBar);

            this.Controls.Add(mRecord);
            this.Controls.Add(mFullImage);
            this.Controls.Add(mPathToWatchLabel);
            this.Controls.Add(mPathToWatchBox);

        }

        private CameraSlider MakeTrackBar(CameraSliderType pos)
        {
            CameraSlider newSlider = new CameraSlider(pos);
            newSlider.Location = new System.Drawing.Point(10, 10 + (int)pos * 65);
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

    }


    public class CameraSlider : Panel
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
                case CameraSliderType.timelapse:
                    name = "Timelapse frame interval";
                    break;
                case CameraSliderType.DisplayInterval:
                    name = "Display Interval";
                case CameraSliderType.FeedVidLength:
                    name = "FeedVidLength";
                    break;
                default:
                    name = "";
                    break;
            }
            mLabel.Text = name;


            mTrackbar = new TrackBar();
            mTrackbar.Location = new System.Drawing.Point(SLIDERINITX, SLIDERINITY);
            mTrackbar.Name = "bar";
            mTrackbar.Size = new System.Drawing.Size(SLIDERWIDTH, SLIDERHEIGHT);
            mTrackbar.TabIndex = 1;

            mValue = new Label();
            mValue.AutoSize = true;
            mValue.Location = new System.Drawing.Point(SLIDERWIDTH + 10, SLIDERINITY);
            mValue.Name = "label2";
            mValue.Size = new System.Drawing.Size(35, 13);
            mValue.TabIndex = 2;
            mValue.Text = "";

            this.Size = new System.Drawing.Size(SLIDERWIDTH + 40, SLIDERHEIGHT + 20);


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
        timelapse,
        FeedVidLength,
        DisplayInterval
    }
}
