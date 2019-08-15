using UnityEngine;
using UnityEngine.SceneManagement;

using Windows.Kinect;

namespace DepthVisor.Kinect
{
    // Based on the the MultiSourceManager class from the Kinect SDK Unity package
    public class KinectManager : MonoBehaviour
    {
        public CoordinateMapper Mapper { get; private set; }

        public int ColourFrameWidth { get; private set; }
        public int ColourFrameHeight { get; private set; }
        public Texture2D ColourTexture { get; private set; }

        public int DepthFrameWidth { get; private set; }
        public int DepthFrameHeight { get; private set; }
        public ushort[] DepthData { get; private set; }

        private KinectSensor sensor;
        private MultiSourceFrameReader multiSourceReader;

        private byte[] colourData;
        private bool firstFrameArrived;
        private bool subscribedFrameHandler;

        void Awake()
        {
            // Establish a connection with the connect sensor
            sensor = KinectSensor.GetDefault();
            if (DoesSensorExist())
            {
                // Open a frame reader for the colour and depth data; then store a reference to the coordinate mapper
                multiSourceReader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
                Mapper = sensor.CoordinateMapper;

                // Retrieve the properties of the colour frame and store the frame dimensions
                FrameDescription colorFrameDesc = sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
                ColourFrameWidth = colorFrameDesc.Width;
                ColourFrameHeight = colorFrameDesc.Height;

                // Initialise an empty 2D texture based on these dimensions and an empty byte array for the
                // actual pixel data
                colourData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];
                ColourTexture = new Texture2D(ColourFrameWidth, ColourFrameHeight, TextureFormat.RGBA32, false);

                // Do the same for the depth frames
                FrameDescription depthFrameDesc = sensor.DepthFrameSource.FrameDescription;
                DepthFrameWidth = depthFrameDesc.Width;
                DepthFrameHeight = depthFrameDesc.Height;

                // Also initialise an empty unsigned short array for the depth pixel data
                DepthData = new ushort[depthFrameDesc.LengthInPixels];

                // To only perform data processing in the Update method once data is available, subscribe a one-shot
                // event handler to the frame arrived event in the reader that will unsubscribe itself once triggered.
                // When this event is triggered, the handler will flip a flag to indicate that the first valid frame
                // has arrived. The other flag is then used to keep track of if the handler is currently subscribed to
                // the event or not
                firstFrameArrived = false;
                multiSourceReader.MultiSourceFrameArrived += FirstFrameArrivedHandler;
                subscribedFrameHandler = true;

                // Open the sensor if it is closed to begin capturing data
                if (!sensor.IsOpen)
                {
                    sensor.Open();
                }

                // Finally, add a handler to the scene unloaded event to close all Kinect resources when the scene
                // is changed
                SceneManager.sceneUnloaded += SceneUnloadedHandler;
            }
        }

        void Update()
        {
            // If the sensor reference does not exist or it is not ready, ensure that the frame
            // arrived flags are reset and then cancel the update
            if (!DoesSensorExist() || !IsSensorReady())
            {
                // If the frame handler is not already subscribed to the frame arrived event, do
                // this and then flip the flag
                if (!subscribedFrameHandler)
                {
                    multiSourceReader.MultiSourceFrameArrived += FirstFrameArrivedHandler;
                    subscribedFrameHandler = true;
                }

                if (firstFrameArrived) { firstFrameArrived = false; }

                return;
            }

            // Otherwise, if the sensor is available and ready but the first frame is yet to arrive,
            // simply return until it has arrived
            if (!firstFrameArrived)
            {
                return;
            }

            // If all above checks have passed, the current frame of incoming data can be processed and
            // made available to other classes
            MultiSourceFrame newMultiFrame = multiSourceReader.AcquireLatestFrame();
            if (newMultiFrame != null)
            {
                ColorFrame colourFrame = newMultiFrame.ColorFrameReference.AcquireFrame();
                if (colourFrame != null)
                {
                    DepthFrame depthFrame = newMultiFrame.DepthFrameReference.AcquireFrame();
                    if (depthFrame != null)
                    {
                        // If all frame references are valid, first generate the 2D texture from the current
                        // colour image by copying the frame pixel data into the byte array. Then, load
                        // this into the texture and apply the changes to render them
                        colourFrame.CopyConvertedFrameDataToArray(colourData, ColorImageFormat.Rgba);
                        ColourTexture.LoadRawTextureData(colourData);
                        ColourTexture.Apply();

                        // Copy the current depth frame data into the ushort array
                        depthFrame.CopyFrameDataToArray(DepthData);

                        // Finally, once all data has been copied, dispose of all frames and dereference
                        // them to free resources for the next incoming frame
                        depthFrame.Dispose();
                        depthFrame = null;
                    }

                    colourFrame.Dispose();
                    colourFrame = null;
                }

                newMultiFrame = null;
            }
        }

        void OnApplicationQuit()
        {
            CloseResources();
        }

        public bool DoesSensorExist()
        {
            return sensor != null;
        }

        public bool IsSensorReady()
        {
            return sensor.IsAvailable;
        }

        public bool IsDataAvailable()
        {
            return firstFrameArrived;
        }

        private void FirstFrameArrivedHandler(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            firstFrameArrived = true;

            // Unsubscribe itself from the event and then flip the flag in this class to indicate
            // that it is no longer subscribed
            ((MultiSourceFrameReader)sender).MultiSourceFrameArrived -= FirstFrameArrivedHandler;
            subscribedFrameHandler = false;
        }

        private void SceneUnloadedHandler(Scene currentScene)
        {
            // Close Kinect resources and remove the handler from the event
            CloseResources();
            SceneManager.sceneUnloaded -= SceneUnloadedHandler;
        }

        // Explicitly dereference the sensor, mapper and reader, also disposing of
        // the reader if it has been initialised and closing the sensor as well
        private void CloseResources()
        {
            // If no Kinect has ever been available, the dispose method on the reader
            // can throw an exception if it has not been initialised, so catch this
            if (multiSourceReader != null)
            {
                try
                {
                    multiSourceReader.Dispose();
                }
                catch (System.InvalidOperationException e)
                {
                    Debug.LogException(e, this); // TODO: Better error handling?
                }

                multiSourceReader = null;
            }

            if (Mapper != null)
            {
                Mapper = null;
            }

            if (sensor != null)
            {
                if (sensor.IsOpen)
                {
                    sensor.Close();
                }

                sensor = null;
            }
        }
    }
}
