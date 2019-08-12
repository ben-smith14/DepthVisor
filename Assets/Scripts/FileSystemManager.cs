using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DepthVisor.Recording
{
    public class FileSystemManager
    {
        public event EventHandler SerializationFinished;

        private string depthVisorDataPath;

        public FileSystemManager()
        {
            depthVisorDataPath = Path.Combine("C:\\Users", "bensm", "Documents", "DepthVisorSaves");
        }

        public void SerializeAndSave(KinectRecordingStore serializableData, string fileName)
        {
            if (!Directory.Exists(depthVisorDataPath))
            {
                Directory.CreateDirectory(depthVisorDataPath);
            }

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fileStream = new FileStream(GetSavePath(fileName), FileMode.Create))
            {
                using (GZipStream compressStream = new GZipStream(fileStream, CompressionLevel.Fastest))
                {
                    // TODO : Needs better error handling
                    try
                    {
                        formatter.Serialize(compressStream, serializableData);
                        SerializationFinished.Invoke(this, new EventArgs());
                    }
                    catch (SerializationException)
                    {
                        throw new SerializationException("Error in serializing recording data");
                    }
                    catch (IOException)
                    {
                        throw new IOException("Error in writing data to file");
                    }
                }
            }
        }

        public static void DeserializeAndLoad()
        {

        }

        private string GetSavePath(string name)
        {
            return Path.Combine(depthVisorDataPath, name + ".dvrec");
        }
    }
}
