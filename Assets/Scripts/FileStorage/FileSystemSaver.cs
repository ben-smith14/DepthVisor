using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

using DepthVisor.Kinect;

namespace DepthVisor.FileStorage
{
    public class FileSystemSaver : FileSystem
    {
        private FileInfo fileInfo;

        void Awake()
        {
            depthVisorSavePath = PlayerPrefs.GetString("savePath");
            fileInfo = new FileInfo(0, 0, 0, KinectMeshGenerator.DepthScale, KinectMeshGenerator.DepthMinReliableDistance, KinectMeshGenerator.DepthMaxReliableDistance);
        }

        public void SetFramesPerChunk(short framesPerChunk)
        {
            fileInfo.FramesPerChunk = framesPerChunk;
        }

        public bool IsMeshDimensionsSet()
        {
            return (fileInfo.MeshWidth != 0 && fileInfo.MeshHeight != 0);
        }

        public void SetMeshDimensions(int meshHeight, int meshWidth)
        {
            fileInfo.MeshHeight = meshHeight;
            fileInfo.MeshWidth = meshWidth;
        }

        public void SerializeAndSaveFileChunk(string fileName, KinectFramesStore serializableData)
        {
            // Check that the specified file exists
            if (!DoesFileExist(fileName))
            {
                throw new FileNotFoundException("Could not find the specified file");
            }

            // Use a binary formatter and memory stream to first serialize the incoming
            // chunk. Then, use QuickLZ to compress this data before writing it into the
            // file
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                try
                {
                    formatter.Serialize(memoryStream, serializableData);
                }
                catch (SerializationException)
                {
                    throw new SerializationException("Error in serializing kinect data");
                }

                // Reset the memory stream to its beginning and then compress the serialized object.
                // Level 1 gives a higher compression ration and Level 3 gives faster decompression
                memoryStream.Position = 0;
                byte[] compressedBytes = QuickLZ.compress(memoryStream.ToArray(), 3);

                // Append the chunk onto the file, also recording the size of the new chunk using the
                // stream position before and after the write so that it can be stored in the file info
                // object
                long startPosition, endPosition;
                using (FileStream fileStream = new FileStream(GetFullFilePath(fileName), FileMode.Append, FileAccess.Write))
                {
                    startPosition = fileStream.Position;

                    try
                    {
                        fileStream.Write(compressedBytes, 0, compressedBytes.Length);
                    }
                    catch (IOException)
                    {
                        throw new IOException("Could not write chunk data to file");
                    }

                    endPosition = fileStream.Position;
                }

                // Add a new chunk size to the file info object's internal list using the stream
                // position differences
                fileInfo.ChunkSizes.Add(endPosition - startPosition);
            }
        }

        public void SerializeAndSaveFileInfo(string fileName, float recordingLengthInSecs)
        {
            // Check that the specified file exists and that the recording length input is valid
            if (!DoesFileExist(fileName))
            {
                throw new FileNotFoundException("Could not find the specified file");
            }
            else if (recordingLengthInSecs < 0.0f)
            {
                throw new ArgumentOutOfRangeException("Recording length must be a positive value");
            }

            // Add the final recording length to the file info object to complete it
            fileInfo.TotalRecordingLength = recordingLengthInSecs;

            long startPosition, endPosition;
            string fullFilePath = GetFullFilePath(fileName);

            // Use similar code to the saving of chunks to serialize the data, compress it and then
            // append it onto the end of the save file
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                try
                {
                    formatter.Serialize(memoryStream, fileInfo);
                }
                catch (SerializationException)
                {
                    throw new SerializationException("Error in serializing file information");
                }

                memoryStream.Position = 0;
                byte[] compressedBytes = QuickLZ.compress(memoryStream.ToArray(), 3);

                using (FileStream fileStream = new FileStream(fullFilePath, FileMode.Append, FileAccess.Write))
                {
                    startPosition = fileStream.Position;

                    try
                    {
                        fileStream.Write(compressedBytes, 0, compressedBytes.Length);
                    }
                    catch (IOException)
                    {
                        throw new IOException("Could not write file information to file");
                    }

                    endPosition = fileStream.Position;
                }
            }

            // TODO : Delete
            Debug.Log("Footer size: " + (endPosition - startPosition));

            // Finally, reopen the file using a binary writer and append the file info object size onto its end.
            // The file has to be reopened like this because opening multiple writers on the same stream causes
            // errors
            using (BinaryWriter writer = new BinaryWriter(File.Open(fullFilePath, FileMode.Append, FileAccess.Write)))
            {
                try
                {
                    writer.Write(endPosition - startPosition);
                }
                catch (IOException)
                {
                    throw new IOException("Error in writing file footer size to the file");
                }
            }
        }
    }
}
