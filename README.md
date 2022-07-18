# Eye Tracker Form
This is a C# based tool for tracking the position of the deep pseudopupil in the *Drosophila* eye.

## Usage
You will need a FLIR camera pointed at a fly eye that is illuminated to give a good image of the deep pseudopupil.  

Launch the program by clicking on the file `EyeTrackerForm` in `EyeTrackerForm\EyeTrackerForm\bin\Debug\` 

Select your camera using `file->add camera`

This should launch your camera in the video view.  You now need to adjust the ROI using the top, bottom, left, and right sliders. The ROI should include at least all possible places the deep pseudopupil will go, with some room around the edges.

Now turn on the tracker by checking the box labeled `record`. You will have to select a rig number and experiment number, these will determine where you data gets saved.

Now you need to adjust the threshold so that the deep pseudopupil is well segmented from the rest of the image.  Check the box labeled `thresholded image` to bring up the thresholded image, and adjust the threshold value until there is a well defined pseudopupil.  It's very important that the pseudopupil area does not touch any of the edges or any of the thresholded areas that do touch the edges, regardless of where it is in its movement range, otherwise it will not work.

Now the tracker should be running and saving. You can go to whichever view is most useful for your experiment (thresholded or not, full image or not.)

## Installation
Currently, this tool must bu built using VisualStudio. Start by installing [Visual Studio](https://visualstudio.microsoft.com/), and making sure you check the box for .NET desktop development.

You will also need to install the [Spinnaker SDK from FLIR](https://www.flir.com/products/spinnaker-sdk/?vertical=machine+vision&segment=iis)

Launch the project by double clicking on the file `EyeTrackerForm.sln` This should launch Visual Studio.

Build the project by clicking on the start button in the top center of the window. The dropdowns next to it should say `Debug` and `Any CPU`.  This should start the EyeTrackerForm in another window. 

The project should now be built and you can close the new window and close Visual Studio.

## Configuration

Before running the first time, adjust the config file to match your setup and preferences.

The config file can be found at `EyeTrackerForm\EyeTrackerForm\bin\Debug\EyeTrackerForm.exe.config`, and it should look like below:

At the very least you need to correct the `ouptut_path_root` to something that makes sense on your machine.

The `time_lapse_interval` will change how often a frame is recorded to the timelapse video that the program saves, and the `rig_list` will change the options that show up in the rig list dropdown when saving data.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/>
    </startup>
<appSettings>
    <add key="output_path_root" value="C:\Users\maimon\eyeTracking"/>
    <add key="rig_list" value="rig1;rig2;rig3"/>
    <!--record a video frame once every time_lapse_interval frames (set to 1 for constant video)-->
    <add key="time_lapse_interval" value="150"/>
    <!--Display once per display_interval frames-->
    <add key="display_interval" value="1"/>
</appSettings>
</configuration>
```