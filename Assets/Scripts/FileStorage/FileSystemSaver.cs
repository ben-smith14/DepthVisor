using System;
using System.IO;
using System.Text;
using System.IO.Compression;
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

            // Use a binary formatter, file stream and gzip stream to serialize and compress the data down
            // so that it can be appended onto the save file. Also record the size of the new chunk using
            // the stream position before and after to store it in the file info object
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(GetFullFilePath(fileName), FileMode.Append))
            {
                using (GZipStream compressStream = new GZipStream(fileStream, System.IO.Compression.CompressionLevel.Fastest))
                {
                    long startPosition = fileStream.Position;

                    try
                    {
                        formatter.Serialize(compressStream, serializableData);
                    }
                    catch (SerializationException)
                    {
                        throw new SerializationException("Error in serializing kinect data");
                    }

                    // The file stream position will be at the next free position, so backtrack by one
                    // to get the last byte position of the serialized object
                    long endPosition = (fileStream.Position - 1);

                    // Add a new chunk size to the file info object's internal list using the stream
                    // position differences
                    fileInfo.ChunkSizes.Add(endPosition - startPosition);
                }
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

            // Use a binary formatter, file stream and gzip stream once again to serialize and compress the
            // info object and append it onto the file. Record the size of the object using the stream
            // positions during this
            long startPosition, endPosition;
            BinaryFormatter formatter = new BinaryFormatter();
            string fullFilePath = GetFullFilePath(fileName);
            using (FileStream fileStream = new FileStream(fullFilePath, FileMode.Append))
            {
                using (GZipStream compressStream = new GZipStream(fileStream, System.IO.Compression.CompressionLevel.Fastest))
                {   
                    startPosition = fileStream.Position;

                    try
                    {
                        formatter.Serialize(compressStream, fileInfo);
                    }
                    catch (SerializationException)
                    {
                        throw new SerializationException("Error in serializing file info");
                    }

                    endPosition = fileStream.Position - 1;
                }
            }

            Debug.Log("Footer size: " + (endPosition - startPosition));

            // Finally, reopen the file using a binary writer and append the file info object size onto its end.
            // The file has to be reopened like this because opening multiple writers on the same stream causes
            // errors
            using (BinaryWriter writer = new BinaryWriter(File.Open(fullFilePath, FileMode.Append)))
            {
                try
                {
                    writer.Write(endPosition - startPosition);
                }
                catch (IOException)
                {
                    throw new IOException("Error in writing file information to the file");
                }
            }
        }
    }
}
