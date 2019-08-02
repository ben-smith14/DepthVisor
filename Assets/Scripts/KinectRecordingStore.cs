using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DepthVisor.Recording
{
    [System.Serializable]
    public class KinectRecordingStore
    {
        // STUFF TO DO WITH SINGLETONS, MAY WANT TO MOVE TO A DIFFERENT PLACE
        // LIKE THE RECORDING MANAGER

        //public static KinectRecordingStorage Instance { get; private set; }
        //void Awake()
        //{
        //    if (Instance != null && Instance != this)
        //    {
        //        Destroy(gameObject);
        //    }
        //    else
        //    {
        //        Instance = this;
        //    }
        //}

        private FrameDetails frameDimensions;
        private List<KinectFrame> frames;

        public KinectRecordingStore(int colourWidth, int colourHeight, int depthWidth, int depthHeight, int downSampling)
        {
            frameDimensions = new FrameDetails(colourWidth, colourHeight, depthWidth, depthHeight, downSampling);
            frames = new List<KinectFrame>();
        }

        public void AddFrame(float[] depthData, byte[] rawColour, Vector2[] uvs)
        {
            frames.Add(new KinectFrame(depthData, rawColour, uvs));
        }

        public void ResetRecording()
        {
            frames = new List<KinectFrame>();
        }

        // Store the dimensions of each frame so that it can be used to reconstruct the recording later
        // (COULD STORE THIS IN META-DATA INSTEAD)
        private struct FrameDetails
        {
            public FrameDetails(int colourWidth, int colourHeight, int depthWidth, int depthHeight, int downSampling)
            {
                ColourFrameWidth = colourWidth;
                ColourFrameHeight = colourHeight;
                DepthFrameWidth = depthWidth;
                DepthFrameHeight = depthHeight;
                DownSampling = downSampling;
            }

            public int ColourFrameWidth;
            public int ColourFrameHeight;
            public int DepthFrameWidth;
            public int DepthFrameHeight;
            public int DownSampling;
        }

        private class KinectFrame
        {
            public float[] DepthData { get; private set; }
            public byte[] RawColour { get; private set; }
            public Vector2[] Uvs { get; private set; }

            public KinectFrame(float[] depthData, byte[] rawColour, Vector2[] uvs)
            {
                DepthData = depthData; // Z dimensions of each vertex
                RawColour = rawColour; // Texture2D converted to raw byte array
                Uvs = uvs; // 2D vector map between texture and vertices
            }
        }
    }
}
