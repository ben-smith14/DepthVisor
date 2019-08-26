using System;
using System.IO;
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

            // Open the recording file, move the position in the stream to the head of the last long value
            // in the file and read it back using a binary reader. Then, use its value to set the position
            // of the file stream to the head of the compressed info footer and read this back into a byte
            // array
            FileInfo fileInfo;
            byte[] fileInfoCompressed;
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

            // Using a binary formatter, memory stream and the quick lz class, decompress the byte array and write
            // it to the memory stream. Then, use this to deserialize the file info object, storing a reference to
            // it and then returning this
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] decompressedBytes = QuickLZ.decompress(fileInfoCompressed);

                memoryStream.Write(decompressedBytes, 0, decompressedBytes.Length);
                memoryStream.Position = 0;

                fileInfo = (FileInfo)formatter.Deserialize(memoryStream);
            }

            return fileInfo;
        }

        public KinectFramesStore DeserializeAndLoadFileChunk(string fileName, long chunkStartPosition, long chunkSize)
        {
            // Open the requested save file, seek to the starting position of the chunk and read in data
            // up to the chunk size. Then, decompress this byte array and write it to a memory stream
            // so that this can be passed to the formatter and deserialized into a frame store object
            KinectFramesStore chunk;
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (FileStream fileStream = new FileStream(GetFullFilePath(fileName), FileMode.Open))
                {
                    byte[] compressedChunk = new byte[chunkSize];

                    fileStream.Seek(chunkStartPosition, SeekOrigin.Begin);
                    fileStream.Read(compressedChunk, 0, compressedChunk.Length);

                    byte[] decompressedChunk = QuickLZ.decompress(compressedChunk);
                    memoryStream.Write(decompressedChunk, 0, decompressedChunk.Length);
                }

                memoryStream.Position = 0;
                chunk = (KinectFramesStore)formatter.Deserialize(memoryStream);
            }

            return chunk;
        }
    }
}
