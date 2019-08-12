using System.Collections.Generic;
using UnityEngine;

namespace DepthVisor.Recording
{
    [System.Serializable]
    public class KinectRecordingStore
    {
        // STUFF TO DO WITH SINGLETONS, MAY WANT TO MOVE TO A DIFFERENT PLACE
        // LIKE THE RECORDING MANAGER? OR KEEP HERE?

        // LOOK AT DONTDESTROYONLOAD FOR THIS CLASS

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

        private List<KinectFrame> frames;

        public KinectRecordingStore()
        {
            frames = new List<KinectFrame>();
        }

        public void AddFrame(Vector3[] vertices, Texture2D colourTexture, Vector2[] uvs)
        {
            // Extract the depth of each vertex into a float array and convert the uv
            // coordinates list into a list of objects that can be serialized
            float[] depthData = new float[vertices.Length];
            SerializableVector2[] serializableUvs = new SerializableVector2[uvs.Length];

            // The vertex and uv arrays are the same length, so the new arrays can be
            // populated in the same for loop
            for (int i = 0; i < vertices.Length; i++)
            {
                depthData[i] = vertices[i].z;
                serializableUvs[i] = new SerializableVector2(uvs[i]);
            }

            // Create a new Kinect frame using the data and add it to the storage object
            frames.Add(new KinectFrame(depthData,
                                       ImageConversion.EncodeToJPG(colourTexture),
                                       serializableUvs));
        }

        public void ResetRecording()
        {
            frames = new List<KinectFrame>();
        }

        [System.Serializable]
        private class KinectFrame
        {
            public float[] DepthData { get; private set; }
            public byte[] CompressedTexture { get; private set; }
            public SerializableVector2[] Uvs { get; private set; }

            public KinectFrame(float[] depthData, byte[] compressedTexture, SerializableVector2[] uvs)
            {
                DepthData = depthData; // Array of Z dimensions for each vertex
                CompressedTexture = compressedTexture; // Texture2D encoded into compressed PNG byte array
                Uvs = uvs; // 2D vector map between texture and vertices

                // TODO : For debugging file sizes in bytes
                //Debug.Log("DepthData: " + DepthData.Length * 4 + " RawColour: " + CompressedTexture.Length + " Uvs: " + Uvs.Length * 2 * 4);
            }
        }

        [System.Serializable]
        private class SerializableVector2
        {
            public float X { get; private set; }
            public float Y { get; private set; }

            public SerializableVector2(Vector2 vectorToConvert)
            {
                X = vectorToConvert.x;
                Y = vectorToConvert.y;
            }
        }
    }
}
