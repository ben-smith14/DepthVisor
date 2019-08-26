using System;
using System.Collections.Generic;

using UnityEngine;

using DepthVisor.FileStorage;
using DepthVisor.Playback;

namespace DepthVisor.Kinect
{
    public class KinectMeshPlayback : MonoBehaviour
    {
        [Header("Mesh Status UI Message")]
        [SerializeField] GameObject MeshStatusTextContainer = null;

        [Header("Playback Manager")]
        [SerializeField] PlaybackManager PlaybackManager = null;

        [Header("Mesh Parameters")]
        [Range(2.0f, 25.0f)] [SerializeField] float TriEdgeThreshold = 10.0f;

        private enum MeshState
        {
            StartingUp,
            NoFile,
            InitialisingMesh,
            RenderingMesh
        }

        private const int chunkBufferSize = 1;

        private MeshState currentMeshState;
        private Renderer meshRenderer;
        private Mesh mesh;
        private Vector3[] vertices;
        private Vector2[] uv;
        private List<int> thresholdTris;
        private Queue<KinectFramesStore> chunkBuffer;
        private KinectFramesStore currentChunk;

        private int frameIndex;
        private float frameTimeDelta;
        private bool preventSkipFlag;

        void Start()
        {
            // Retrieve a reference to the mesh renderer and the mesh object in the mesh filter
            meshRenderer = gameObject.GetComponent<Renderer>();
            mesh = gameObject.GetComponent<MeshFilter>().mesh;

            // Initialise the chunk buffer queue
            chunkBuffer = new Queue<KinectFramesStore>();

            // Set the mesh state to the no file state until it is initialised
            SetMeshState(MeshState.NoFile);
        }

        void Update()
        {
            // As mesh initialisation happens all in one frame, if the mesh state indicates that
            // it is initialising, call the associated method and then change it to the rendering
            // state
            if (currentMeshState == MeshState.InitialisingMesh)
            {
                InitialisePlayback(PlaybackManager.GetNextChunk(), PlaybackManager.OpenFileInfo);
                SetMeshState(MeshState.RenderingMesh);
            }

            //// If the playback manager has initialised a file, first check the mesh state
            //if (PlaybackManager.FileReady)
            //{
            //    // If the mesh state indicates no file, initialise the mesh by changing state
            //    if (currentMeshState == MeshState.NoFile)
            //    {
            //        SetMeshState(MeshState.InitialisingMesh);
            //        return;
            //    }
            //    else if (currentMeshState == MeshState.InitialisingMesh)
            //    {
            //        // Otherwise, if the mesh has been initialised, begin rendering it by changing
            //        // state once again
            //        SetMeshState(MeshState.RenderingMesh);
            //        return;
            //    }

            //    // If the mesh is rendering and the user is playing the recording, begin updating
            //    // frames using the chunks that have been loaded in
            //    if (currentMeshState == MeshState.RenderingMesh && PlaybackManager.IsPlaying)
            //    {
            //        CheckForMeshUpdate();
            //    }

            //    // TODO : Load in more chunks if not enough in buffer
            //    if (chunkBuffer.Count < chunkBufferSize)
            //    {
            //        PlaybackManager.GetNextChunk();
            //    }
            //}
        }

        public void ShowAndInitialiseMesh()
        {
            SetMeshState(MeshState.InitialisingMesh);
        }

        private void InitialisePlayback(KinectFramesStore firstChunk, FileSystem.FileInfo fileInfo)
        {
            // Store the input chunk
            currentChunk = firstChunk;

            // Initialise an empty downsampled mesh as a 2D plane with the correct height and width
            // attributes that match the recording images
            InitialiseMeshData(fileInfo.MeshWidth, fileInfo.MeshHeight);

            // Align the centre of the mesh to its parent container origin, which by default is world
            // space
            AlignMeshToWorldOrigin(fileInfo.MeshWidth, fileInfo.MeshHeight, fileInfo.DepthScale, fileInfo.MinReliableDistance, fileInfo.MaxReliableDistance);

            // Initialise the timer between frames and the frame indexer
            frameTimeDelta = 0.0f;
            frameIndex = 0;

            // Load in the first frame
            RefreshMeshData(fileInfo.MeshWidth, fileInfo.MeshHeight, currentChunk[frameIndex]);
        }

        private void InitialiseMeshData(int meshWidth, int meshHeight)
        {
            // Create a new mesh object and set it as the component for the game object
            mesh = new Mesh();

            // Initialise the data structures for the mesh and use a temporary triangles
            // array, which won't be filtered for edge length at this stage
            vertices = new Vector3[meshWidth * meshHeight];
            uv = new Vector2[meshWidth * meshHeight];
            int[] triangles = new int[6 * (meshWidth - 1) * (meshHeight - 1)];

            int triangleIndex = 0;
            for (int y = 0; y < meshHeight; y++)
            {
                for (int x = 0; x < meshWidth; x++)
                {
                    // Get the single index for the pixel arrays, which starts in the top left of
                    // incoming images and sweeps across each row from left to right, wrapping
                    // around to subsequent rows as it goes downwards
                    int index = (y * meshWidth) + x;

                    // Fill the vertex and uv arrays, inverting the y values in world space to display
                    // the mesh in the correct orientation and normalising the UV coordinates
                    vertices[index] = new Vector3(x, -y, 0);
                    uv[index] = new Vector2(x / meshWidth, y / meshHeight);

                    // Fill the triangles array using the index position of the vertices that make up
                    // each of their corners to initialise the whole grid
                    if (x != (meshWidth - 1) && y != (meshHeight - 1))
                    {
                        int topLeft = index;
                        int topRight = topLeft + 1;
                        int bottomLeft = topLeft + meshWidth;
                        int bottomRight = bottomLeft + 1;

                        triangles[triangleIndex++] = topLeft;
                        triangles[triangleIndex++] = topRight;
                        triangles[triangleIndex++] = bottomLeft;

                        triangles[triangleIndex++] = bottomLeft;
                        triangles[triangleIndex++] = topRight;
                        triangles[triangleIndex++] = bottomRight;
                    }
                }
            }

            // Load in the data to the mesh object and recalculate normals to ensure lighting
            // and shading is correct
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
        }

        private void AlignMeshToWorldOrigin(int meshWidth, int meshHeight, float depthScale, short minDepth, short maxDepth)
        {
            // Based on the Kinect parameters, depth scale and downsampling value, we can
            // first establish the centre of the mesh in in its local space
            Vector3 meshLocalCentre = new Vector3(0, 0, 0)
            {
                x = (meshWidth) / 2,
                y = -(meshHeight) / 2,
                z = (maxDepth - minDepth) * depthScale / 2
            };

            // Then, we can adjust for the minimum reliable distance that the Kinect can gather
            // data from, as the mesh is slightly offset from its actual origin by this amount
            meshLocalCentre += new Vector3(0, 0, minDepth * depthScale);

            // Finally, we can move the game object origin back to the world origin and then
            // align its centre with the world origin by adding the local centre vector to this
            // translation
            gameObject.transform.position -= (gameObject.transform.position + meshLocalCentre);
        }

        private void RefreshMeshData(int meshWidth, int meshHeight, KinectFramesStore.KinectFrame frame)
        {
            // Load the compressed JPEG byte array into a new texture and then assign this to the
            // mesh main texture
            Texture2D colourTexture = new Texture2D(0, 0);
            ImageConversion.LoadImage(colourTexture, frame.CompressedColour);
            meshRenderer.material.mainTexture = colourTexture;

            // Initialise an empty triangles list to dynamically store the valid triangles
            // for rendering the mesh
            thresholdTris = new List<int>();

            // For each vertex row:
            for (int y = 0; y < meshHeight; y++)
            {
                // Define a vector3 array for storing quad corners and a float array for storing
                // quad edges
                Vector3[] quadCorners = new Vector3[4];
                float[] quadEdges = new float[5];

                // Then, for each vertex across each row:
                for (int x = 0; x < meshWidth; x++)
                {
                    // Get the index of the current vertex
                    int index = (y * meshWidth) + x;

                    // Set the vertex's depth and uv map point
                    vertices[index].z = frame.DepthData[index];
                    uv[index] = new Vector2(frame.Uvs[index].X, frame.Uvs[index].Y);

                    // Define triangles if the height index is past the first row of vertices and the width
                    // index is on at least the second vertex of the current row
                    if (y >= 1 && x >= 1)
                    {
                        // Get the small index of the top left corner of the quad, as the actual small index
                        // is currently on the bottom right
                        int triTopLeftIndex = index - 1 - meshWidth;

                        // Get each of the vectors that make up the corners of the quad
                        quadCorners[0] = vertices[triTopLeftIndex]; // top left
                        quadCorners[1] = vertices[triTopLeftIndex + 1]; // top right
                        quadCorners[2] = vertices[index - 1]; // bottom left
                        quadCorners[3] = vertices[index]; // bottom right

                        // Find the length of all edges in the quad (4 outer edges and one diagonal through
                        // the middle)
                        quadEdges[0] = Vector3.Distance(quadCorners[0], quadCorners[1]); // top
                        quadEdges[1] = Vector3.Distance(quadCorners[1], quadCorners[2]); // diagonal
                        quadEdges[2] = Vector3.Distance(quadCorners[2], quadCorners[0]); // left
                        quadEdges[3] = Vector3.Distance(quadCorners[1], quadCorners[3]); // right
                        quadEdges[4] = Vector3.Distance(quadCorners[3], quadCorners[2]); // bottom

                        // If all edges of the first triangle in the quad are less than or equal to the threshold
                        // value, add the triangle to the list (define points clockwise to prevent back face culling)
                        if (quadEdges[0] <= TriEdgeThreshold && quadEdges[1] <= TriEdgeThreshold && quadEdges[2] <= TriEdgeThreshold)
                        {
                            // Add triangle using top left corner index, top right and bottom left
                            thresholdTris.Add(triTopLeftIndex);
                            thresholdTris.Add(triTopLeftIndex + 1);
                            thresholdTris.Add(index - 1);
                        }

                        // Do the same for the second triangle in the quad
                        if (quadEdges[1] <= TriEdgeThreshold && quadEdges[3] <= TriEdgeThreshold && quadEdges[4] <= TriEdgeThreshold)
                        {
                            // Add triangle using bottom left corner index, top right and bottom right
                            thresholdTris.Add(index - 1);
                            thresholdTris.Add(triTopLeftIndex + 1);
                            thresholdTris.Add(index);
                        }
                    }
                }
            }

            // Load in the new data to the mesh, converting the triangle list to
            // an array of vertex indices
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = thresholdTris.ToArray();
            mesh.RecalculateNormals();
        }

        private void CheckForMeshUpdate()
        {
            // Add the time from the last frame onto the timer variable
            frameTimeDelta += Time.deltaTime;

            try
            {
                // The time delta stored with each frame is the time between itself and
                // the frame before it, so when looking to load in a new frame, we need to
                // check the time of the next frame in the chunk. However, if we are moving
                // from the last frame in a chunk to a the first frame in a new chunk and
                // the frame time was not long enough to trigger a refresh of the mesh in
                // the catch block, using the next frame along would skip the first frame
                // in this new chunk. Therefore, a flag is used to indicate if we need to
                // prevent skipping by using the time delta of the current chunk instead
                float tempTimeDelta;
                if (preventSkipFlag)
                {
                    tempTimeDelta = currentChunk[currentChunk.FrameIndex].FrameDeltaTime;
                }
                else
                {
                    tempTimeDelta = currentChunk[currentChunk.FrameIndex + 1].FrameDeltaTime;
                }

                // If the timer count is greater than or equal to the time between this frame and
                // the next, load in the next frame and reset the associated variables
                if (frameTimeDelta >= tempTimeDelta)
                {
                    RefreshMeshData(PlaybackManager.OpenFileInfo.MeshWidth, PlaybackManager.OpenFileInfo.MeshHeight, currentChunk.NextFrame());
                    if (preventSkipFlag) { preventSkipFlag = false; }
                    frameTimeDelta = 0.0f;
                }
            }
            catch (IndexOutOfRangeException)
            {
                // If we try to access the next frame in the chunk and we are at the end of the chunk,
                // an exception will be thrown, so catch this and start using a new chunk
                if (chunkBuffer.Count > 0)
                {
                    // If there are chunks available in the buffer, assign one to the current chunk,
                    // and fetch a new chunk to place in the buffer from the playback manager
                    currentChunk = chunkBuffer.Dequeue();
                    chunkBuffer.Enqueue(PlaybackManager.GetNextChunk());

                    // Then, perform the same check on the timings as before and refresh the mesh data
                    // if this check is valid
                    if (frameTimeDelta >= currentChunk[currentChunk.FrameIndex].FrameDeltaTime)
                    {
                        RefreshMeshData(PlaybackManager.OpenFileInfo.MeshWidth, PlaybackManager.OpenFileInfo.MeshHeight, currentChunk[currentChunk.FrameIndex]);
                        if (preventSkipFlag) { preventSkipFlag = false; }
                        frameTimeDelta = 0.0f;
                    }
                    else
                    {
                        // Otherwise, to prevent skipping of the first frame in the new chunk, flip the
                        // associated flag
                        preventSkipFlag = true;
                    }
                }
                else
                {
                    // TODO : Do this here or in canvas manager?
                    PlaybackManager.StopPlaying();
                }
            }
        }

        private void SetMeshState(MeshState newState)
        {
            // If the mesh state has not changed, return
            if (currentMeshState == newState)
            {
                return;
            }

            // Otherwise, make changes to the UI based on the new state
            switch (newState)
            {
                // If the mesh is in the no file state, show the mesh status message and disable the mesh
                // renderer
                case MeshState.NoFile:
                    if (!MeshStatusTextContainer.activeSelf) { MeshStatusTextContainer.SetActive(true); }
                    if (meshRenderer.enabled) { meshRenderer.enabled = false; }
                    break;
                // If the mesh is in the initialising state, show the mesh status message and disable the mesh
                // renderer again, but also trigger the method to initialise the mesh
                case MeshState.InitialisingMesh:
                    if (!MeshStatusTextContainer.activeSelf) { MeshStatusTextContainer.SetActive(true); }
                    if (meshRenderer.enabled) { meshRenderer.enabled = false; }
                    break;
                // For the rendering mesh state, hide the mesh status message if visible, then enable the
                // mesh renderer if not visible
                case MeshState.RenderingMesh:
                    if (MeshStatusTextContainer.activeSelf) { MeshStatusTextContainer.SetActive(false); }
                    if (!meshRenderer.enabled) { meshRenderer.enabled = true; }

                    // TODO : Do I need this??
                    preventSkipFlag = false;
                    break;
            }

            // Store the new state
            currentMeshState = newState;
        }
    }
}
