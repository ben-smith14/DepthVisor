using System.Collections.Generic;
using System.Threading;

using UnityEngine;

using DepthVisor.Kinect;
using System;

namespace DepthVisor.Recording
{
    public class RecordingManager : MonoBehaviour
    {
        [SerializeField] GameObject KinectView;
        [SerializeField] int FramesPerStore = 50;

        public bool IsProcessingFile { get; private set; }

        private KinectManager kinectManager;
        private KinectMeshGenerator kinectMesh;
        private FileSystemManager fileManager;

        private KinectFramesStore tempKinectFrameStore;
        private Queue<KinectFramesStore> recordedDataQueue;

        private string currentFileName;
        private bool isRecording;

        void Start()
        {
            // Cache references to the kinect components that will contain the data
            // that needs to be stored
            kinectManager = KinectView.GetComponent<KinectManager>();
            kinectMesh = KinectView.GetComponent<KinectMeshGenerator>();

            // Cache a reference to the file manager and add the backgrouns save handler
            // to the finished serialization event
            fileManager = gameObject.GetComponent<FileSystemManager>();
            fileManager.FinishedSerialization += BackgroundSaveFinishedHandler;

            // Initialise the temporary kinect store object and the queue of store objects
            tempKinectFrameStore = new KinectFramesStore(FramesPerStore);
            recordedDataQueue = new Queue<KinectFramesStore>();

            // Initialise the flags
            isRecording = false;
            IsProcessingFile = false;
        }        

        void Update()
        {
            // If currently recording, try to add the next frame data to the current temporary
            // frame store
            if (isRecording)
            {
                Vector3[] meshVertices = kinectMesh.GetMeshVertices();
                Texture2D colourTexture = kinectManager.ColourTexture;
                Vector2[] uvs = kinectMesh.GetMeshUvs();

                try
                {
                    tempKinectFrameStore.AddFrame(meshVertices, colourTexture, uvs);
                }
                catch (KinectFramesStore.FrameStoreFullException)
                {
                    // If an exception is thrown, the temporary store is full, so copy it into
                    // the data queue
                    recordedDataQueue.Enqueue(new KinectFramesStore(tempKinectFrameStore));

                    // Then, reinitialise the temporary store and add the current frame to the
                    // new object
                    tempKinectFrameStore = new KinectFramesStore(FramesPerStore);
                    tempKinectFrameStore.AddFrame(meshVertices, colourTexture, uvs);
                }
            }

            // If the background thread has not yet started and the data queue is not empty,
            // add the file serialize and save callback to the thread pool work queue, then
            // flip the file processing flag
            if (!IsProcessingFile && recordedDataQueue.Count != 0)
            {
                ThreadPool.QueueUserWorkItem(FileSaveCallback, recordedDataQueue.Dequeue());
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

        // Callback for the thread pool work queue that uses the file manager's serialize and
        // save method to append the next set of frames onto the current file
        private void FileSaveCallback(object callbackData)
        {
            fileManager.SerializeAndSave((KinectFramesStore)callbackData, currentFileName);
        }

        // Handler that is triggered when the file manager's serialize and save method completes
        // its current job
        private void BackgroundSaveFinishedHandler(object sender, EventArgs e)
        {
            // If there are more items in the data queue, add the next one to the thread pool's
            // work queue. This ensures that the background thread accessing the file is blocked
            // for each frame store object and that they are only being saved one at a time
            if (recordedDataQueue.Count != 0)
            {
                ThreadPool.QueueUserWorkItem(FileSaveCallback, recordedDataQueue.Dequeue());
            }
            else
            {
                // Otherwise, the queue has been cleared, so set the file processing flag to off
                IsProcessingFile = false;
            }
        }
    }
}
