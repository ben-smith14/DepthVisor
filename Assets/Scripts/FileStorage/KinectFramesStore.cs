using System;
using System.Collections;
using System.Runtime.Serialization;

using UnityEngine;

namespace DepthVisor.FileStorage
{
    [Serializable]
    public class KinectFramesStore : ISerializable, IEnumerator
    {
        public int MaxFramesCount { get; private set; }

        private KinectFrame[] frames;
        private int frameIndex;

        public KinectFramesStore(int maxFramesCount)
        {
            // Standard constructor
            frames = new KinectFrame[maxFramesCount];
            MaxFramesCount = maxFramesCount;
            frameIndex = 0;
        }

        public KinectFramesStore(KinectFramesStore toCopy)
        {
            // Constructor for creating a deep copy of another frame store
            frames = toCopy.frames;
            MaxFramesCount = toCopy.MaxFramesCount;
            frameIndex = 0;
        }

        public KinectFramesStore(SerializationInfo info, StreamingContext context)
        {
            // Deserialize values constructor
            frames = (KinectFrame[])info.GetValue("framesArray", typeof(KinectFrame[]));
            MaxFramesCount = frames.Length;
            frameIndex = 0;
        }

        public void AddFrame(Vector3[] vertices, Texture2D colourTexture, Vector2[] uvs, float frameDeltaTime)
        {
            // If the frame count has reached its limit, throw an exception
            if (frameIndex >= MaxFramesCount)
            {
                throw new FrameStoreFullException("Frame count is equal to the maximum");
            }

            // Then, extract the depth of each vertex into a float array and convert
            // the uv coordinates list into a list of objects that can be serialized
            float[] depthData = new float[vertices.Length];
            KinectFrame.SerializableVector2[] serializableUvs = new KinectFrame.SerializableVector2[uvs.Length];

            // The vertex and uv arrays are the same length, so the new arrays can be
            // populated in the same for loop
            for (int i = 0; i < vertices.Length; i++)
            {
                depthData[i] = vertices[i].z;
                serializableUvs[i] = new KinectFrame.SerializableVector2(uvs[i]);
            }
            
            // Create a new Kinect frame using the data and add it to the storage object. Encode
            // the colour texture as a JPEG to reduce file size
            frames[frameIndex++] = new KinectFrame(depthData,
                                                   ImageConversion.EncodeToJPG(colourTexture),
                                                   serializableUvs,
                                                   frameDeltaTime);
        }

        // Implementation for ISerializable
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("framesArray", frames, typeof(KinectFrame[]));
        }

        // Remaining methods implement IEnumerator
        public bool MoveNext()
        {
            // If the frame index is past the last position, move it back to the last position
            // (which is just after the last element) and return false
            if (frameIndex >= MaxFramesCount)
            {
                frameIndex = MaxFramesCount;
                return false;
            }
            else if (frameIndex == MaxFramesCount-1)
            {
                // If the frame index is one behind the last position, move it to the last
                // position, but return false to indicate there is no current value
                frameIndex++;
                return false;
            }
            else
            {
                // Otherwise, just move the index position up by one and return true
                frameIndex++;
                return true;
            }
        }

        public bool MovePrev()
        {
            // If the frame index is past the first position, reset it and return false
            if (frameIndex < 0)
            {
                Reset();
                return false;
            }
            else if (frameIndex == 0)
            {
                // If the frame index is one position in front of the first position, move
                // it to the first position, but return false to indicate that there is no
                // current value
                frameIndex--;
                return false;
            }
            else
            {
                // Otherwise, simply move the index back one position
                frameIndex--;
                return true;
            }
        }

        public void Reset()
        {
            frameIndex = -1;
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public KinectFrame Current
        {
            // Try to retrieve the current value pointed at by the iterator. If it is not
            // on a valid value, throw an exception
            get
            {
                try
                {
                    return frames[frameIndex];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException("Frame index pointer is not in a valid position");
                }
            }
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
}
