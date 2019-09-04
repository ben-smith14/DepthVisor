using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using DepthVisor.FileStorage;

namespace DepthVisor.Playback
{
    public class PlaybackManager : MonoBehaviour
    {
        [Header("Loading Parameters")]
        [SerializeField] short RefreshBufferSize = 4;
        [SerializeField] short ChunkBufferSize = 2;

        public event EventHandler FileInfoFinishedLoad;
        public event EventHandler ChunkFinishedLoad;

        public bool IsLoading { get; private set; }
        public bool IsPlaying { get; private set; }
        public FileSystem.FileInfo FileInfoOpen { get; private set; }

        private FileSystemLoader fileLoader;
        private Queue<ChunkLoadData> chunksToLoadQueue;
        private Queue<KinectFramesStore> loadedChunkQueue;

        private int fileChunkIndex;
        private string currentFileName;

        void Start()
        {
            // Initialise all local variables
            fileLoader = gameObject.GetComponent<FileSystemLoader>();
            chunksToLoadQueue = new Queue<ChunkLoadData>();
            loadedChunkQueue = new Queue<KinectFramesStore>();
            IsLoading = false;
            IsPlaying = false;

            // Subscribe event handlers to event
            FileInfoFinishedLoad += FileInfoLoadFinishedHandler;
            ChunkFinishedLoad += LoadChunkFinishedHandler;
        }

        public void StartPlaying()
        {
            IsPlaying = true;
        }

        public void StopPlaying()
        {
            IsPlaying = false;
        }

        public int GetChunksToLoadCount()
        {
            return chunksToLoadQueue.Count;
        }

        public KinectFramesStore GetNextChunk()
        {
            // First check to see if any chunks are available in the queue, returning
            // a null reference if there aren't
            if (loadedChunkQueue.Count == 0)
            {
                return null;
            }

            // Otherwise, dequeue the next chunk
            KinectFramesStore nextChunk = loadedChunkQueue.Dequeue();

            // If the file chunk index is not at the last position, there is more data
            // left in the file
            if (fileChunkIndex < FileInfoOpen.ChunkSizes.Count-1)
            {
                // If the loaded chunk queue is now equla to or under the buffer size,
                // stop playback and load in more chunks to refresh the buffer
                if (loadedChunkQueue.Count <= ChunkBufferSize)
                {
                    StopPlaying();

                    // Check that the number of chunks left in the file is greater than the number
                    // of chunks to load in to refill the buffer. If it is not, use the remaining
                    // number of chunks in the file instead
                    int lastChunkIndex = fileChunkIndex;
                    if (RefreshBufferSize > (FileInfoOpen.ChunkSizes.Count - 1 - fileChunkIndex))
                    {
                        fileChunkIndex = FileInfoOpen.ChunkSizes.Count - 1;
                    }
                    else
                    {
                        fileChunkIndex += RefreshBufferSize;
                    }

                    // If a chunk is still loading as well, simply queue up all of these new chunks
                    if (IsLoading)
                    {
                        for (int i = lastChunkIndex + 1; i < fileChunkIndex + 1; i++)
                        {
                            chunksToLoadQueue.Enqueue(new ChunkLoadData(GetChunkStartFromIndex(i), FileInfoOpen.ChunkSizes[i]));
                        }
                    }
                    else
                    {
                        // Otherwise, if there is more than one chunk to load, skip over the first chunk
                        // in this new set and queue up all of the others, including the chunk that the
                        // global index is currently on
                        if (fileChunkIndex - lastChunkIndex > 1)
                        {
                            for (int i = lastChunkIndex + 2; i < fileChunkIndex + 1; i++)
                            {
                                chunksToLoadQueue.Enqueue(new ChunkLoadData(GetChunkStartFromIndex(i), FileInfoOpen.ChunkSizes[i]));
                            }
                        }

                        // Then, begin loading in the first of these new chunks
                        ThreadPool.QueueUserWorkItem(ChunkLoadCallback,
                            new ChunkLoadData(GetChunkStartFromIndex(lastChunkIndex + 1), FileInfoOpen.ChunkSizes[lastChunkIndex + 1]));
                    }
                }
                else
                {
                    // Otherwise, progress the file chunk index and load in the next chunk to replace
                    // the one just dequeued
                    fileChunkIndex++;
                    ChunkLoadData newChunkToLoad = new ChunkLoadData(GetChunkStartFromIndex(fileChunkIndex),
                        FileInfoOpen.ChunkSizes[fileChunkIndex]);

                    // If another chunk is already loading, add this new chunk into the chunks to load
                    // queue. Otherwise, manually queue the chunk processing task on the background thread
                    if (IsLoading)
                    {
                        chunksToLoadQueue.Enqueue(newChunkToLoad);
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(ChunkLoadCallback, newChunkToLoad);
                    }
                }
            }
            else if (IsLoading)
            {
                // Otherwise, if it is at the last position and loading data in, stop playing so that the
                // final chunks can be added to the buffer before resuming playing
                StopPlaying();
            }

            return nextChunk;
        }
            
        public void OpenFile(string fileName)
        {
            // Reset the queues and the playing flag for the new file
            chunksToLoadQueue = new Queue<ChunkLoadData>();
            loadedChunkQueue = new Queue<KinectFramesStore>();
            IsPlaying = false;

            // Flip the is loading flag and store the file name to open
            IsLoading = true;
            currentFileName = fileName;

            // Then, begin loading in the file info
            ThreadPool.QueueUserWorkItem(FileInfoLoadCallback, currentFileName);
        }

        private long GetChunkStartFromIndex(int chunkIndex)
        {
            if (FileInfoOpen == null)
            {
                throw new NullReferenceException("No file info available");
            }
            else if (chunkIndex > (FileInfoOpen.ChunkSizes.Count - 1) || chunkIndex < 0)
            {
                throw new ArgumentOutOfRangeException("Provided chunk index is not within the index range of the file info");
            }

            // Convert the chunk's index position to its starting byte position in
            // the file by adding up all previous chunk sizes
            long chunkStartBytes = 0;
            for (int i = 0; i < chunkIndex; i++)
            {
                chunkStartBytes += FileInfoOpen.ChunkSizes[i];
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
                FileInfoOpen = fileLoader.DeserializeAndLoadFileInfo((string)callbackData);
            }
            catch (ArgumentOutOfRangeException e)
            {
                // An exception will indicate that the file is smaller than the minimum size
                throw new ArgumentOutOfRangeException(e.Message);
            }

            // Trigger this event
            FileInfoFinishedLoad.Invoke(this, new EventArgs());
        }

        // Handler that is triggered when the file info has been loaded
        private void FileInfoLoadFinishedHandler(object sender, EventArgs e)
        {
            // Check that the number of chunks in the file is greater than the initial number
            // of chunks to load. If it is not, use the number of chunks in the file, otherwise
            // use the initial chunks to load variable
            if (RefreshBufferSize > FileInfoOpen.ChunkSizes.Count)
            {
                fileChunkIndex = FileInfoOpen.ChunkSizes.Count - 1;
            }
            else
            {
                fileChunkIndex = RefreshBufferSize - 1;
            }

            // If there is more than one chunk to load, queue up chunk loading callbacks with
            // the relevant data items for all chunks other than the first
            if (fileChunkIndex > 0)
            {
                for (int i = 1; i < fileChunkIndex + 1; i++)
                {
                    chunksToLoadQueue.Enqueue(new ChunkLoadData(GetChunkStartFromIndex(i), FileInfoOpen.ChunkSizes[i]));
                }
            }

            // Then, begin loading in the first chunk
            ThreadPool.QueueUserWorkItem(ChunkLoadCallback, new ChunkLoadData(GetChunkStartFromIndex(0), FileInfoOpen.ChunkSizes[0]));
        }

        private void ChunkLoadCallback(object callbackData)
        {
            // Cast the callback object to the data object, serialize and load the chunk using this data
            // and then add it to the loaded chunk queue
            ChunkLoadData chunk = (ChunkLoadData)callbackData;
            loadedChunkQueue.Enqueue(fileLoader.DeserializeAndLoadFileChunk(currentFileName, chunk.ChunkByteIndex, chunk.ChunkSize));

            // Finally, invoke the chunk finished event
            ChunkFinishedLoad.Invoke(this, new EventArgs());
        }

        // Handler that is triggered when a chunk has been loaded
        private void LoadChunkFinishedHandler(object sender, EventArgs e)
        {
            // If there are more chunks in the load queue, dequeue the next chunk
            // to load object and add its callback with data to the thread pool for
            // execution
            if (chunksToLoadQueue.Count != 0)
            {
                ChunkLoadData chunk = chunksToLoadQueue.Dequeue();
                ThreadPool.QueueUserWorkItem(ChunkLoadCallback, chunk);
            }
            else
            {
                // Otherwise, the manager has finished loading data, so flip the
                // relevant flags
                IsLoading = false;
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
