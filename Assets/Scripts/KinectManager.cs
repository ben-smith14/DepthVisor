using UnityEngine;
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
        private bool unsubFirstFrameHandler;

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
                // event handler to the frame arrived event in the reader. When this event is triggered for the first
                // time, this handler will flip a flag to indicate that the first valid frame has arrived and then the
                // other flag will be used to unsubscribe the handler to prevent further unecessary triggering
                firstFrameArrived = false;
                unsubFirstFrameHandler = false;
                multiSourceReader.MultiSourceFrameArrived += FirstFrameArrived;

                // Finally, open the sensor if it is closed to begin capturing data
                if (!sensor.IsOpen)
                {
                    sensor.Open();
                }
            }
        }

        private void FirstFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            firstFrameArrived = true;
        }

        void Update()
        {
            // If the sensor reference does not exist or it is not ready, cancel the update
            if (!DoesSensorExist() || !IsSensorReady())
            {
                firstFrameArrived = false;
                return;
            }

            // Otherwise, if the first frame is yet to arrive, flip the unsubscribe first frame flag if
            // not already true to indicate that the event handler will need to be unsubscribed when it
            // does arrive
            if (!firstFrameArrived)
            {
                if (!unsubFirstFrameHandler)
                {
                    unsubFirstFrameHandler = true;
                }

                return;
            } else if (firstFrameArrived && unsubFirstFrameHandler)
            {
                // If the first frame has arrived and the unsubscribe first frame flag is still true,
                // unsubscribe the handler to prevent unecessary future triggers and flip the flag back
                // to false to skip over this condition in the future
                multiSourceReader.MultiSourceFrameArrived -= FirstFrameArrived;
                unsubFirstFrameHandler = false;
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

        // Dereference the sensor, mapper and reader on application quit,
        // also explicitly disposing of the reader and closing the sensor
        // as well
        void OnApplicationQuit()
        {
            if (multiSourceReader != null)
            {
                multiSourceReader.Dispose();
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
