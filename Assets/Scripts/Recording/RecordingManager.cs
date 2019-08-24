using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using DepthVisor.Kinect;
using DepthVisor.FileStorage;
using DepthVisor.UI;

namespace DepthVisor.Recording
{
    public class RecordingManager : MonoBehaviour
    {
        [SerializeField] GameObject KinectView = null;
        [SerializeField] RecordingTimerManager timerManager = null;
        [SerializeField] short FramesPerStore = 50;

        public event EventHandler ChunkFinishedSerialization;
        public event EventHandler FileInfoFinished;

        public bool IsProcessingFile { get; private set; }

        private KinectManager kinectManager;
        private KinectMeshGenerator kinectMesh;
        private FileSystemSaver fileSaver;

        private KinectFramesStore tempKinectFrameStore;
        private Queue<KinectFramesStore> recordedDataQueue;

        private string currentFileName;
        private bool firstFrame;
        private bool isRecording;

        void Start()
        {
            // Cache references to the kinect components that will contain the data
            // that needs to be stored
            kinectManager = KinectView.GetComponent<KinectManager>();
            kinectMesh = KinectView.GetComponent<KinectMeshGenerator>();

            // Cache a reference to the file manager and set its frames per chunk info
            // attribute
            fileSaver = gameObject.GetComponent<FileSystemSaver>();
            fileSaver.SetFramesPerChunk(FramesPerStore);

            // Add the background save handlers to the finished serialization event
            // and finished file info event
            ChunkFinishedSerialization += ChunkSaveFinishedHandler;
            FileInfoFinished += FileInfoFinishedHandler;

            // Initialise the temporary kinect store object and the queue of store objects
            tempKinectFrameStore = new KinectFramesStore(FramesPerStore);
            recordedDataQueue = new Queue<KinectFramesStore>();

            // Initialise the flags
            isRecording = false;
            firstFrame = true;
            IsProcessingFile = false;
        }        

        void Update()
        {
            // If currently recording, try to add the next frame data to the current temporary
            // frame store
            if (isRecording)
            {
                // If the file info in the file saver does not have the mesh dimensions set,
                // set these using the kinect manager
                if (!fileSaver.IsMeshDimensionsSet())
                {
                    fileSaver.SetMeshDimensions(
                        kinectManager.DepthFrameHeight/KinectMeshGenerator.DownSampleSize,
                        kinectManager.DepthFrameWidth/KinectMeshGenerator.DownSampleSize);
                }

                // Retrieve all of the mesh data for storage
                Vector3[] meshVertices = kinectMesh.GetMeshVertices();
                Texture2D colourTexture = kinectManager.ColourTexture;
                Vector2[] uvs = kinectMesh.GetMeshUvs();

                // If this is the first frame being added, set the time since the last frame to 0
                // and flip the flag so that all subsequent times are then the time between update
                // frames
                float timeSinceLastFrame;
                if (firstFrame)
                {
                    timeSinceLastFrame = 0.0f;
                    firstFrame = false;
                }
                else
                {
                    timeSinceLastFrame = Time.deltaTime;
                }

                try
                {
                    tempKinectFrameStore.AddFrame(meshVertices, colourTexture, uvs, timeSinceLastFrame);
                }
                catch (KinectFramesStore.FrameStoreFullException)
                {
                    // If an exception is thrown, the temporary store is full, so copy it into
                    // the data queue
                    recordedDataQueue.Enqueue(new KinectFramesStore(tempKinectFrameStore));

                    // Then, reinitialise the temporary store and add the current frame to the
                    // new object
                    tempKinectFrameStore = new KinectFramesStore(FramesPerStore);
                    tempKinectFrameStore.AddFrame(meshVertices, colourTexture, uvs, timeSinceLastFrame);
                }
            }

            // If the background thread has not yet started and the data queue is not empty,
            // add the file serialize and save callback to the thread pool work queue, then
            // flip the file processing flag
            if (!IsProcessingFile && recordedDataQueue.Count != 0)
            {
                ThreadPool.QueueUserWorkItem(SaveChunkCallback, recordedDataQueue.Dequeue());
                IsProcessingFile = true;
            }
        }

        public void StartRecording(string fileName)
        {
            currentFileName = fileName;
            isRecording = true;
        }

        public void StopRecording()
        {
            isRecording = false;
        }

        public short GetFramesCountPerChunk()
        {
            return FramesPerStore;
        }

        public int GetDataQueueCount()
        {
            return recordedDataQueue.Count;
        }

        // Callback for the thread pool work queue that uses the file manager's serialize and
        // save method to append the next chunk of frames onto the current file
        private void SaveChunkCallback(object callbackData)
        {
            fileSaver.SerializeAndSaveFileChunk(currentFileName, (KinectFramesStore)callbackData);

            // Trigger the chunk finished serialization event
            ChunkFinishedSerialization.Invoke(this, new EventArgs());
        }

        // Callback for the thread pool work queue that uses the file manager's serialize and
        // save method to append the next chunk of frames onto the current file
        private void SaveInfoCallback(object callbackData)
        {
            fileSaver.SerializeAndSaveFileInfo(currentFileName, (float)callbackData);

            // Trigger the file info finished event
            FileInfoFinished.Invoke(this, new EventArgs());
        }

        // Handler that is triggered when the file manager's serialize and save method completes
        // its current job
        private void ChunkSaveFinishedHandler(object sender, EventArgs e)
        {
            // If there are more items in the data queue, add the next one to the thread pool's
            // work queue. This ensures that the background thread accessing the file is blocked
            // for each frame store object and that they are only being saved one at a time
            if (recordedDataQueue.Count != 0)
            {
                ThreadPool.QueueUserWorkItem(SaveChunkCallback, recordedDataQueue.Dequeue());
            }
            else
            {
                // Otherwise, the queue has been cleared and all recording data has been saved, so
                // serialize and save the file footer information to finish
                ThreadPool.QueueUserWorkItem(SaveInfoCallback, timerManager.TimerCount);
            }
        }

        // Handler that is triggered when the file info has been serialized and saved
        private void FileInfoFinishedHandler(object sender, EventArgs e)
        {
            IsProcessingFile = false;
        }
    }
}
