using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

namespace DepthVisor.Recording
{
    public class FileSystemManager : MonoBehaviour
    {
        public event EventHandler FinishedSerialization;

        private string depthVisorSavePath;
        private const string depthVisorSaveFolder = "DepthVisorSaves";
        private const string fileExtension = ".dvrec";

        void Start()
        {
            depthVisorSavePath = PlayerPrefs.GetString("savePath");
        }

        public void CreateSaveDirectoryIfNotExists()
        {
            string directoryPath = GetFullSavePath();
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public bool DoesFileExist(string fileName)
        {
            return File.Exists(GetFullFilePath(fileName));
        }

        public void CreateOrOverwriteFile(string fileName)
        {
            File.Create(GetFullFilePath(fileName));
        }

        public IEnumerable<string> GetFileList()
        {
            // Get the full file paths from the directory
            IEnumerable<string> fullFilePaths = Directory.EnumerateFiles(GetFullSavePath(), "*" + fileExtension);

            // For each file path in this enumerable, extract the filename without the file
            // extension and store in a string array
            string[] fileNames = new string[fullFilePaths.Count()];
            int i = 0;
            foreach (string filePath in fullFilePaths)
            {
                fileNames[i++] = Path.GetFileName(filePath).Split(new char[] { '.' })[0];
            }

            // Return the file names as an enumerable type
            return fileNames.AsEnumerable();
        }

        public void SerializeAndSave(KinectFramesStore serializableData, string fileName)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(GetFullFilePath(fileName), FileMode.Append))
            {
                // TODO : Try LZ4 compression for better speed
                //using (GZipStream compressStream = new GZipStream(fileStream, System.IO.Compression.CompressionLevel.Fastest))
                //{
                    // TODO : Needs better error handling
                    try
                    {
                        formatter.Serialize(fileStream, serializableData);
                    }
                    catch (SerializationException)
                    {
                        throw new SerializationException("Error in serializing recording data");
                    }
                    catch (IOException)
                    {
                        throw new IOException("Error in writing data to file");
                    }
                //}
            }

            FinishedSerialization.Invoke(this, new EventArgs());
        }

        public void DeserializeAndLoad()
        {

        }

        private string GetFullSavePath()
        {
            return Path.Combine(depthVisorSavePath, depthVisorSaveFolder);
        }

        private string GetFullFilePath(string fileName)
        {
            return Path.Combine(GetFullSavePath(), fileName + fileExtension);
        }
    }
}
