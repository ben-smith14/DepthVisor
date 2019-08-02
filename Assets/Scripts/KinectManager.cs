using System;
using UnityEngine;
using Windows.Kinect;

namespace DepthVisor.Kinect
{
    // Largely based on the the MultiSourceManager class from the Kinect SDK Unity package
    public class KinectManager : MonoBehaviour
    {
        public CoordinateMapper Mapper { get; private set; }
        public int ColourFrameWidth { get; private set; }
        public int ColourFrameHeight { get; private set; }
        public Texture2D ColourTexture { get; private set; }
        public int DepthFrameWidth { get; private set; }
        public int DepthFrameHeight { get; private set; }
        public ushort[] DepthData { get; private set; }

        public event EventHandler<MultiSourceFrameArrivedEventArgs> NewDataArrived;

        private KinectSensor sensor;
        private MultiSourceFrameReader multiSourceReader;
        private byte[] colourData;

        void Awake()
        {
            // Establish a connection to the Kinect before generating a mesh
            sensor = KinectSensor.GetDefault();
            if (sensor != null)
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
                ColourTexture = new Texture2D(ColourFrameWidth, ColourFrameHeight, TextureFormat.RGBA32, false);
                colourData = new byte[colorFrameDesc.BytesPerPixel * colorFrameDesc.LengthInPixels];

                // Do the same for the depth frames
                FrameDescription depthFrameDesc = sensor.DepthFrameSource.FrameDescription;
                DepthFrameWidth = depthFrameDesc.Width;
                DepthFrameHeight = depthFrameDesc.Height;

                // And also initialise an empty unsigned short array for the depth pixel data
                DepthData = new ushort[depthFrameDesc.LengthInPixels];

                // Finally, before opening the sensor to begin capturing data, add two event handlers to
                // the multi source frame arrived event. One that will prepare and store the new data in
                // a better format; another that will trigger a custom new event for other classes to
                // subscribe to
                multiSourceReader.MultiSourceFrameArrived += PrepareNewFrameData;
                multiSourceReader.MultiSourceFrameArrived += TriggerNewDataEvent;

                if (!sensor.IsOpen)
                {
                    sensor.Open();
                }
            }
        }

        public bool IsSensorNull()
        {
            return sensor == null;
        }

        public CoordinateMapper GetCoordinateMapper()
        {
            return sensor.CoordinateMapper;
        }

        private void PrepareNewFrameData(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // TODO : DO I NEED TO UPDATE THE FRAME DIMENSIONS EVERYTIME?
            MultiSourceFrame newMultiFrame = e.FrameReference.AcquireFrame();
            if (newMultiFrame != null)
            {
                ColorFrame colourFrame = newMultiFrame.ColorFrameReference.AcquireFrame();
                if (colourFrame != null)
                {
                    DepthFrame depthFrame = newMultiFrame.DepthFrameReference.AcquireFrame();
                    if (depthFrame != null)
                    {
                        // If all frames are available, first generate the 2D texture from the current
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

        private void TriggerNewDataEvent(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // Trigger the custom event to notify all subscribers by instantiating it if
            // it is not null (i.e. if it has any subscribers)
            EventHandler<MultiSourceFrameArrivedEventArgs> triggerEvent = NewDataArrived;
            if (triggerEvent != null)
            {
                NewDataArrived(this, e);
            }
        }

        // Dereference the sensor and reader on application quit, also
        // explicitly disposing of the reader and closing the sensor as
        // well
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
