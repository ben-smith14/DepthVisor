using System;
using System.Runtime.Serialization;

using UnityEngine;

namespace DepthVisor.FileStorage
{
    [Serializable]
    public class KinectFramesStore : ISerializable
    {
        public int MaxFramesCount { get; private set; }
        public int FrameIndex { get; private set; }

        private KinectFrame[] frames;

        public KinectFramesStore(int maxFramesCount)
        {
            // Standard constructor
            frames = new KinectFrame[maxFramesCount];
            MaxFramesCount = maxFramesCount;
            FrameIndex = 0;
        }

        public KinectFramesStore(KinectFramesStore toCopy)
        {
            // Constructor for creating a deep copy of another frame store
            frames = toCopy.frames;
            MaxFramesCount = toCopy.MaxFramesCount;
            FrameIndex = 0;
        }

        public KinectFramesStore(SerializationInfo info, StreamingContext context)
        {
            // Deserialize values constructor
            frames = (KinectFrame[])info.GetValue("framesArray", typeof(KinectFrame[]));
            MaxFramesCount = frames.Length;
            FrameIndex = 0;
        }

        public KinectFrame this[int indexer]
        {
            get
            {
                return frames[indexer];
            }
        }

        public KinectFrame NextFrame()
        {
            try
            {
                FrameIndex++;
                return frames[FrameIndex];
            }
            catch (IndexOutOfRangeException)
            {
                FrameIndex--;
                throw new IndexOutOfRangeException("No additional frames in the storage object");
            }
        }

        public void AddFrame(Vector3[] vertices, Texture2D colourTexture, Vector2[] uvs, float frameDeltaTime)
        {
            // If the frame count has reached its limit, throw an exception
            if (FrameIndex >= MaxFramesCount)
            {
                throw new FrameStoreFullException("Frame count is equal to the maximum");
            }

            // Then, extract the depth of each vertex into a float array and convert
            // the uv coordinates list into a list of objects that can be serialized
            float[] depthData = new float[vertices.Length];
            SerializableVector2[] serializableUvs = new SerializableVector2[uvs.Length];

            // The vertex and uv arrays are the same length, so the new arrays can be
            // populated in the same for loop
            for (int i = 0; i < vertices.Length; i++)
            {
                depthData[i] = vertices[i].z;
                serializableUvs[i] = new SerializableVector2(uvs[i]);
            }

            // TODO : Better way of compressing colour Texture?
            
            // Create a new Kinect frame using the data and add it to the storage object. Encode
            // the colour texture as a JPEG to reduce file size
            frames[FrameIndex++] = new KinectFrame(depthData,
                                                   ImageConversion.EncodeToJPG(colourTexture),
                                                   serializableUvs,
                                                   frameDeltaTime);
        }

        // Implementation for ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("framesArray", frames, typeof(KinectFrame[]));
        }

        // Exception for when the frame store is full and someone tries to add additional
        // frames
        public class FrameStoreFullException : Exception
        {
            public FrameStoreFullException()
            {
            }

            public FrameStoreFullException(string message) : base(message)
            {
            }
        }

        [Serializable]
        public class KinectFrame : ISerializable
        {
            public float[] DepthData { get; private set; }
            public byte[] CompressedColour { get; private set; }
            public SerializableVector2[] Uvs { get; private set; }
            public float FrameDeltaTime { get; private set; }

            public KinectFrame(float[] depthData, byte[] colourImage, SerializableVector2[] uvs, float frameDeltaTime)
            {
                DepthData = depthData; // Array of Z dimensions for each vertex
                CompressedColour = colourImage; // Texture2D encoded into compressed JPEG byte array
                Uvs = uvs; // 2D vector map between texture and vertices
                FrameDeltaTime = frameDeltaTime; // Time between this frame and last frame

                // TODO : For debugging frame sizes; shows them in bytes
                //Debug.Log("DepthData: " + DepthData.Length * 4 + " RawColour: " + CompressedTexture.Length + " Uvs: " + Uvs.Length * 2 * 4 + " Delta Time: 4");
            }

            // Deserialize values constructor
            public KinectFrame(SerializationInfo info, StreamingContext context)
            {
                DepthData = (float[])info.GetValue("frameDepth", typeof(float[]));
                CompressedColour = (byte[])info.GetValue("frameColour", typeof(byte[]));
                Uvs = (SerializableVector2[])info.GetValue("frameUv", typeof(SerializableVector2[]));
                FrameDeltaTime = (float)info.GetValue("frameDeltaTime", typeof(float));
            }

            // Implementation for ISerializable
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("frameDepth", DepthData, typeof(float[]));
                info.AddValue("frameColour", CompressedColour, typeof(byte[]));
                info.AddValue("frameUv", Uvs, typeof(SerializableVector2[]));
                info.AddValue("frameDeltaTime", FrameDeltaTime, typeof(float));
            }
        }

        [Serializable]
        public class SerializableVector2 : ISerializable
        {
            public float X { get; private set; }
            public float Y { get; private set; }

            public SerializableVector2(Vector2 vectorToConvert)
            {
                X = vectorToConvert.x;
                Y = vectorToConvert.y;
            }

            // Deserialize values constructor
            public SerializableVector2(SerializationInfo info, StreamingContext context)
            {
                X = (float)info.GetValue("uvX", typeof(float));
                Y = (float)info.GetValue("uvY", typeof(float));
            }

            // Implementation for ISerializable
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("uvX", X, typeof(float));
                info.AddValue("uvY", Y, typeof(float));
            }
        }
    }
}
