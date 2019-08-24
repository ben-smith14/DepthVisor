using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using DepthVisor.FileStorage;
using DepthVisor.UI;

namespace DepthVisor.Playback
{
    public class PlaybackManager : MonoBehaviour
    {
        [Header("Game Objects")]
        [SerializeField] PlaybackTimesManager timersManager;

        [Header("Loading Parameters")]
        [SerializeField] short InitialChunkLoadSize = 3;
        [SerializeField] short ChunkBufferSize = 2;

        public event EventHandler FileInfoFinishedLoading;
        public event EventHandler ChunkFinishedDeserialization;

        public bool FileReady { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsPlaying { get; private set; }
        public FileSystem.FileInfo OpenFileInfo { get; private set; }

        private FileSystemLoader fileLoader;
        private Queue<ChunkToLoad> chunksToLoadQueue;
        private Queue<KinectFramesStore> loadedChunkQueue;

        private int fileChunkIndex;
        private string currentFileName;

        void Start()
        {
            fileLoader = gameObject.GetComponent<FileSystemLoader>();

            chunksToLoadQueue = new Queue<ChunkToLoad>();
            loadedChunkQueue = new Queue<KinectFramesStore>();
            FileReady = false;
            IsLoading = false;
            IsPlaying = false;

            FileInfoFinishedLoading += FileInfoFinishedLoading;
            ChunkFinishedDeserialization += LoadChunkFinishedHandler;
        }

        void Update()
        {
            
        }

        public void StartPlaying()
        {
            IsPlaying = true;
        }

        public void StopPlaying()
        {
            IsPlaying = false;
        }

        public int GetDataQueueCount()
        {
            return chunksToLoadQueue.Count;
        }

        public int GetChunkQueueCount()
        {
            return loadedChunkQueue.Count;
        }

        public KinectFramesStore GetNextChunk()
        {
            // First check to see if any chunks are available in the queue
            if (loadedChunkQueue.Count == 0)
            {
                throw new InvalidOperationException("No chunks in load queue");
            }

            // Dequeue the next chunk
            KinectFramesStore nextChunk = loadedChunkQueue.Dequeue();

            //// If the queue is now smaller than the buffer size, first check to see if there
            //// any chunks left in the file
            //if (loadedChunkQueue.Count < ChunkBufferSize)
            //{
            //    int chunksLeft = (OpenFileInfo.ChunkSizes.Count - 1) - fileChunkIndex;
            //    if (chunksLeft > 0)
            //    {
            //        // If there are chunks left but a smaller amount than the buffer size, simply
            //        // load in the remaining chunks
            //        if (chunksLeft > ChunkBufferSize)
            //        {
            //            // TODO : for loop that queues up the next load of chunks to fill the buffer
            //            //ChunkBufferSize - loadedChunkQueue.Count
            //        }
            //        else
            //        {
            //            // Otherwise, fill up the buffer with chunks
            //            // TODO : for loop that fills the buffer up as much as it can
            //        }
            //    }
            //}

            //// TODO : Finish off the function
            //ChunkLoadData data = new ChunkLoadData(GetChunkStartFromIndex(fileChunkIndex), OpenFileInfo.ChunkSizes[fileChunkIndex]);
            //if (!IsLoading)
            //{
            //    ThreadPool.QueueUserWorkItem(ChunkLoadCallback, data);
            //} else
            //{
            //    chunksToLoadQueue.Enqueue(new ChunkToLoad(ChunkLoadCallback, data));
            //}

            return nextChunk;
        }
            
        public void OpenFile(string fileName)
        {
            // Flip the is loading flag and store the file name to open
            IsLoading = true;
            currentFileName = fileName;

            // Then, begin loading in the file info
            ThreadPool.QueueUserWorkItem(FileInfoLoadCallback, currentFileName);
        }

        private long GetChunkStartFromIndex(int chunkIndex)
        {
            if (OpenFileInfo == null)
            {
                throw new NullReferenceException("No file info available");
            }
            else if (chunkIndex > (OpenFileInfo.ChunkSizes.Count - 1) || chunkIndex < 0)
            {
                throw new ArgumentOutOfRangeException("Provided chunk index is not within the index range of the file info");
            }

            // Convert the chunk's index position to its starting byte position in
            // the file by adding up all previous chunk sizes
            long chunkStartBytes = 0;
            for (int i = 0; i < chunkIndex; i++)
            {
                chunkStartBytes += OpenFileInfo.ChunkSizes[i];
            }

            // I do not need to add 1 to get the start of the next chunk because the byte array index
            // starts at 0 anyway, so this total will give the total + 1 index position
            return chunkStartBytes;
        }

        private void FileInfoLoadCallback(object callbackData)
        {
            try
            {
                // Try to deserialize and load the file info, storing a reference to it in
                // the manager. Then trigger the file info finished loading event
                OpenFileInfo = fileLoader.DeserializeAndLoadFileInfo((string)callbackData);
            }
            catch (ArgumentOutOfRangeException e)
            {
                // TODO : File is smaller than minimum size, so show error message in options panel
                // Should I move this catch to the canvas??
                throw new ArgumentOutOfRangeException(e.Message);
            }

            FileInfoFinishedLoading.Invoke(this, new EventArgs());
        }

        // Handler that is triggered when the file info has been loaded
        private void FileInfoLoadFinishedHandler(object sender, EventArgs e)
        {
            // Check that the number of chunks in the file is greater than the initial number
            // of chunks to load. If it is not, use the number of chunks in the file, otherwise
            // use the initial chunks to load variable
            if (InitialChunkLoadSize > OpenFileInfo.ChunkSizes.Count)
            {
                fileChunkIndex = OpenFileInfo.ChunkSizes.Count;
            }
            else
            {
                fileChunkIndex = InitialChunkLoadSize;
            }

            // If there is more than one chunk to load, queue up chunk loading callbacks with
            // the relevant data items for all chunks other than the first
            if (fileChunkIndex > 1)
            {
                for (int i = 1; i < fileChunkIndex; i++)
                {
                    ChunkLoadData data = new ChunkLoadData(GetChunkStartFromIndex(i), OpenFileInfo.ChunkSizes[i]);
                    chunksToLoadQueue.Enqueue(new ChunkToLoad(ChunkLoadCallback, data));
                }
            }

            // Then, begin loading in the first chunk
            ThreadPool.QueueUserWorkItem(ChunkLoadCallback, new ChunkLoadData(GetChunkStartFromIndex(0), OpenFileInfo.ChunkSizes[0]));
        }

        private void ChunkLoadCallback(object callbackData)
        {
            // Cast the callback object to the data object, serialize and load the chunk using this data
            // and then add it to the loaded chunk queue
            ChunkLoadData chunk = (ChunkLoadData)callbackData;
            loadedChunkQueue.Enqueue(fileLoader.DeserializeAndLoadFileChunk(currentFileName, chunk.ChunkByteIndex, chunk.ChunkSize));

            // Finally, invoke the chunk finished event
            ChunkFinishedDeserialization.Invoke(this, new EventArgs());
        }

        // Handler that is triggered when a chunk has been loaded
        private void LoadChunkFinishedHandler(object sender, EventArgs e)
        {
            // If there are more chunks in the load queue, dequeue the next chunk
            // to load object and add its callback with data to the thread pool for
            // execution
            if (chunksToLoadQueue.Count != 0)
            {
                ChunkToLoad chunk = chunksToLoadQueue.Dequeue();
                ThreadPool.QueueUserWorkItem(chunk.LoadCallback, chunk.LoadData);
            }
            else
            {
                // Otherwise, the manager has finished loading data, so flip the
                // relevant flags
                if (!FileReady) { FileReady = true; }
                IsLoading = false;
            }
        }

        private class ChunkToLoad
        {
            public WaitCallback LoadCallback { get; private set; }
            public ChunkLoadData LoadData { get; private set; }

            public ChunkToLoad(WaitCallback callback, ChunkLoadData data)
            {
                LoadCallback = callback;
                LoadData = data;
            }
        }

        private class ChunkLoadData
        {
            public long ChunkByteIndex { get; private set; }
            public long ChunkSize { get; private set; }

            public ChunkLoadData(long chunkByteIndex, long chunkSize)
            {
                ChunkByteIndex = chunkByteIndex;
                ChunkSize = chunkSize;
            }
        }
    }
}
