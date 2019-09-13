# DepthVisor

A prototype Unity application that can use the Kinect V2 to record 3D videos. It also allows for basic playback of short files in its current state. The original intention for this project was to record surgical videos that contain depth information.

![Example of DepthVisor Playback](https://github.com/ben-smith14/DepthVisor/blob/master/Kinect_Playback.PNG?raw=true)

**Installation Instructions:**

To build this project, a Windows machine, a Kinect V2 and a Kinect Windows Adapter are required. Use the following steps to install and run the Unity application with this equipment:

1. If you don't have Unity, or the Kinect SDK installed on your computer, download these from [Unity's website](https://unity3d.com/get-unity/download) and the official [Kinect SDK website](https://developer.microsoft.com/en-us/windows/kinect) (Unity 2019.1.5f1 was used to develop the application). The community edition of Unity is fine, but downloading the Unity hub first is the recommended thing to do, as you can then control your downloaded versions of Unity and your local projects through this.

2. To check that the SDK is installed, connect the Kinect V2 to your computer via the Windows Adapter and open Kinect Studio (which comes with the SDK install). Click the "Connect to service" button in Kinect Studio and wait until you can see the images from the Kinect to verify that the data is available.

3. Once this has all been verified, clone the repository or download and extract the .ZIP file onto your local machine.

4. Open this with Unity by opening the Unity Hub, selecting "Add" and then locating the folder that you just cloned/downloaded.

5. Unity will then compile this and open it in the editor. Here you can test the application.

6. When you want to create an executable build, go to "File" -> "Build Settings". Make sure that all of the scenes are in the build, with the main menu first. Also ensure that the build target is set as "PC, Mac & Linux Standalone". Then, select "Build" in the bottom right corner to generate the application files in a specified file location.

7. Once this has finished processing, you can then run the application by double-clicking on the main executable.

8. For the best performance, it is recommended that the Kinect V2 is connected to your computer for at least a few seconds before running the application.

**User Instructions:**

Using the application is very simple once it is installed. There are three main scenes: the main menu, recording and playback.

-> Main Menu - The main menu scene is the landing page for the application. When you first open it, you will have to set a local save location before you can do anything else. However, once this has been done, you can then use it to exit the application or navigate
to the recording and playback scenes.

-> Recording - When you first load this scene, it will display a 3D mesh in real-time after a few seconds if a Kinect is available. To create a new recording, simply select the relevant button, input a filename, press "Done" and then press the "Start Recording" button. If you are overwriting a file, a dialogue box will check this with you. Once you then stop the recording, it will take some time to save before it is automatically opened in the playback scene.

-> Playback - From this scene, if it has been opened automatically by recording, it will take some more time to load the file back in and then the first frame of the video will be shown. Currently, the implementation of the playback scene has not been completed, so only short videos under around 25 seconds can be watched in their entirety. Alternatively, local files can also be opened manually by selecting the relevant button, selecting a file from the list and then selecting the "Open" button. This will trigger the same procedure as before. The upload and download buttons have also not been implemented, along with the fast-forward, rewind and slider functionalities.

**Image references:**

[Surgery background](https://pixabay.com/photos/sci-fi-surgery-room-2992797), [Logo Goggles](https://www.flaticon.com/free-icon/virtual-reality-glasses_1994200#term=vr&page=1&position=72), [Logo Stethoscope](https://www.flaticon.com/free-icon/stethoscope_809957#term=stethoscope&page=1&position=1), [Home Button](https://www.flaticon.com/free-icon/home-icon-silhouette_69524#term=home&page=1&position=4), [Cross Button](https://www.flaticon.com/free-icon/cancel_126497#term=cross&page=1&position=3), [Play Button](https://www.flaticon.com/free-icon/play-button_149125#term=play&page=1&position=2), [Pause Button](https://www.flaticon.com/free-icon/pause_149127#term=pause&page=1&position=1), [Fast-Forward/Rewind](https://www.flaticon.com/free-icon/rewind_149129#term=rewind&page=1&position=2)

**Code references:**

[QuickLZ Script](http://www.quicklz.com/download.html), [Runtime File Browser](https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006)
