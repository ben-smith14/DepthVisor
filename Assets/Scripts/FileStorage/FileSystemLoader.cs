using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

namespace DepthVisor.FileStorage
{
    public class FileSystemLoader : FileSystem
    {
        void Awake()
        {
            depthVisorSavePath = PlayerPrefs.GetString("savePath");
        }

        public FileInfo DeserializeAndLoadFileInfo(string fileName)
        {
            // Check that the file to deserialize exists
            if (!DoesFileExist(fileName))
            {
                throw new FileNotFoundException("Could not find the specified file");
            }

            Debug.Log("Starting retrieval of footer info");

            // Open the recording file, move the position in the stream to the head of the last long value
            // in the file and read it back using a binary reader. Then, use its value to set the position
            // of the file stream to the head of the compressed info footer and read this back into a byte
            // array
            byte[] fileInfoCompressed;
            FileInfo fileInfo;
            using (FileStream fileStream = new FileStream(GetFullFilePath(fileName), FileMode.Open))
            {
                try
                {
                    fileStream.Seek(-sizeof(long), SeekOrigin.End);
                }
                catch (ArgumentException)
                {
                    throw new ArgumentOutOfRangeException("File is smaller than minimum size; is it a " + fileExtension + " file?");
                }

                long footerSize;

                using (BinaryReader reader = new BinaryReader(fileStream, Encoding.UTF8, true))
                {
                    try
                    {
                        footerSize = reader.ReadInt64();
                    }
                    catch (IOException)
                    {
                        throw new IOException("Could not read file info footer size");
                    }
                }

                Debug.Log("Footer size: " + footerSize);

                try
                {
                    fileStream.Seek(-(sizeof(long) + footerSize), SeekOrigin.End);
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new ArgumentOutOfRangeException("File is smaller than minimum size; is it a .dvrec file?");
                }

                fileInfoCompressed = new byte[footerSize];
                fileStream.Read(fileInfoCompressed, 0, fileInfoCompressed.Length);
            }

            Debug.Log("Got footer info");

            // Using a binary formatter, memory stream and gzip stream, write this byte array onto the memory stream and
            // then decompress and deserialize it into a file info object. Store a reference to this in the manager and
            // then return this reference
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(fileInfoCompressed, 0, fileInfoCompressed.Length);
                using (GZipStream decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    // TODO : Bug here where it can't deserialize the footer
                    fileInfo = (FileInfo)formatter.Deserialize(decompressStream);
                }
            }

            Debug.Log("File info decompressed");

            return fileInfo;
        }

        public KinectFramesStore GetChunk(string fileName, float recordingPercentage)
        {
            // TODO : Write this
            return new KinectFramesStore(30);
        }

        public KinectFramesStore DeserializeAndLoadFileChunk(string fileName, long chunkStartPosition, long chunkSize)
        {
            KinectFramesStore chunk;
            BinaryFormatter formatter = new BinaryFormatter();

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (FileStream fileStream = new FileStream(GetFullFilePath(fileName), FileMode.Open))
                {
                    fileStream.Seek(chunkStartPosition, SeekOrigin.Begin);
                    byte[] compressedChunk = new byte[chunkSize];
                    fileStream.Read(compressedChunk, 0, compressedChunk.Length);
                    memoryStream.Write(compressedChunk, 0, compressedChunk.Length);

                    using (GZipStream decompressionStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        chunk = (KinectFramesStore)formatter.Deserialize(decompressionStream);
                    }
                }
            }

            return chunk;
        }
    }
}
