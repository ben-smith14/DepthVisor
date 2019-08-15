using System;
using System.Collections;
using System.Runtime.Serialization;

using UnityEngine;

namespace DepthVisor.Recording
{
    [Serializable]
    public class KinectFramesStore : IEnumerable, ISerializable
    {
        private KinectFrame[] frames;
        private int maxFramesCount;
        private int frameCount;

        public KinectFramesStore(int maxFramesCount)
        {
            // Standard constructor
            this.maxFramesCount = maxFramesCount;
            frameCount = 0;

            frames = new KinectFrame[this.maxFramesCount];
        }

        public KinectFramesStore(KinectFramesStore toCopy)
        {
            // Constructor for creating a deep copy of another frame store
            frames = toCopy.frames;
            maxFramesCount = toCopy.maxFramesCount;
            frameCount = toCopy.frameCount;
        }

        public void AddFrame(Vector3[] vertices, Texture2D colourTexture, Vector2[] uvs)
        {
            // If the frame count has reached its limit, throw an exception
            if (frameCount >= maxFramesCount)
            {
                throw new FrameStoreFullException("Frame count is equal to the maximum");
            }

            // Otherwise, extract the depth of each vertex into a float array and convert
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

            // TODO : Better way of compressing colour Texture??
            // Create a new Kinect frame using the data and add it to the storage object. Encode
            // the colour texture as a JPEG to reduce file size
            frames[frameCount++] = new KinectFrame(depthData,
                                                   ImageConversion.EncodeToJPG(colourTexture),
                                                   serializableUvs);
        }

        // Implementation for ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("framesList", frames, typeof(KinectFrame[]));
        }

        // Implementation for IEnumerable
        IEnumerator IEnumerable.GetEnumerator()
        {
            return frames.GetEnumerator();
        }

        // Exception for when the frame store is full and someone tries to add additional
        // frames
        public class FrameStoreFullException : Exception
        {
            public FrameStoreFullException()
            {
            }

            public FrameStoreFullException(string message)
                : base(message)
            {
            }
        }

        [Serializable]
        public class KinectFrame : ISerializable
        {
            public float[] DepthData { get; private set; }
            public byte[] CompressedColour { get; private set; }
            public SerializableVector2[] Uvs { get; private set; }

            public KinectFrame(float[] depthData, byte[] colourImage, SerializableVector2[] uvs)
            {
                DepthData = depthData; // Array of Z dimensions for each vertex
                CompressedColour = colourImage; // Texture2D encoded into compressed JPG byte array
                Uvs = uvs; // 2D vector map between texture and vertices

                // TODO : For debugging file sizes; shows them in bytes
                //Debug.Log("DepthData: " + DepthData.Length * 4 + " RawColour: " + CompressedTexture.Length + " Uvs: " + Uvs.Length * 2 * 4);
            }

            // Implementation for ISerializable
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("frameDepth", DepthData, typeof(float[]));
                info.AddValue("frameColour", CompressedColour, typeof(byte[]));
                info.AddValue("frameUv", Uvs, typeof(SerializableVector2[]));
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

            // Implementation for ISerializable
            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue("uvX", X, typeof(float));
                info.AddValue("uvY", Y, typeof(float));
            }
        }
    }
}
