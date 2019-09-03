using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

using UnityEngine;

namespace DepthVisor.FileStorage
{
    public class FileSystem : MonoBehaviour
    {
        protected const string depthVisorSaveDirec = "DepthVisorSaves";
        protected const string fileExtension = ".dvrec";

        protected string depthVisorSavePath;

        protected virtual void Awake()
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

        protected string GetFullSavePath()
        {
            return Path.Combine(depthVisorSavePath, depthVisorSaveDirec);
        }

        protected string GetFullFilePath(string fileName)
        {
            return Path.Combine(GetFullSavePath(), fileName + fileExtension);
        }

        [System.Serializable]
        public class FileInfo : ISerializable
        {
            public int MeshWidth { get; set; }
            public int MeshHeight { get; set; }
            public float DepthScale { get; set; }
            public short MinReliableDistance { get; set; }
            public short MaxReliableDistance { get; set; }

            public short FramesPerChunk { get; set; }
            public List<long> ChunkSizes { get; set; }
            public float TotalRecordingLength { get; set; }

            public FileInfo(short framesPerChunk, int meshWidth, int meshHeight, float depthScale,
                short minReliableDistance, short maxReliableDistance)
            {
                MeshWidth = meshWidth;
                MeshHeight = meshHeight;
                DepthScale = depthScale;
                MinReliableDistance = minReliableDistance;
                MaxReliableDistance = maxReliableDistance;

                FramesPerChunk = framesPerChunk;
                ChunkSizes = new List<long>();
                TotalRecordingLength = 0.0f;
            }

            // Deserialize values constructor
            public FileInfo(SerializationInfo info, StreamingContext context)
            {
                MeshWidth = (int)info.GetValue("meshWidth", typeof(int));
                MeshHeight = (int)info.GetValue("meshHeight", typeof(int));
                DepthScale = (float)info.GetValue("depthScale", typeof(float));
                MinReliableDistance = (short)info.GetValue("minDistance", typeof(short));
                MaxReliableDistance = (short)info.GetValue("maxDistance", typeof(short));

                FramesPerChunk = (short)info.GetValue("framesPerChunk", typeof(short));
                ChunkSizes = (List<long>)info.GetValue("chunkSizes", typeof(List<long>));
                TotalRecordingLength = (float)info.GetValue("totalRecordingLength", typeof(float));
            }

            // Serialize values
            public void GetObjectData(SerializationInfo info, StreamingContext stream)
            {
                info.AddValue("meshWidth", MeshWidth, typeof(int));
                info.AddValue("meshHeight", MeshHeight, typeof(int));
                info.AddValue("depthScale", DepthScale, typeof(float));
                info.AddValue("minDistance", MinReliableDistance, typeof(short));
                info.AddValue("maxDistance", MaxReliableDistance, typeof(short));

                info.AddValue("framesPerChunk", FramesPerChunk, typeof(short));
                info.AddValue("chunkSizes", ChunkSizes, typeof(List<long>));
                info.AddValue("totalRecordingLength", TotalRecordingLength, typeof(float));
            }
        }
    }
}
