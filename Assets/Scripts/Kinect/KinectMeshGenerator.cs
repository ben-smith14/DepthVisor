using System.Collections.Generic;

using UnityEngine;
using TMPro;

using Windows.Kinect;

namespace DepthVisor.Kinect
{
    // Based on the DepthSourceView class from the Kinect SDK Unity package
    public class KinectMeshGenerator : MonoBehaviour
    {
        [Header("Mesh Status UI Message")]
        [SerializeField] GameObject MeshStatusTextContainer;
        [SerializeField] string NoSensorText;

        [Header("Mesh Parameters")]
        [Range(2.0f, 20.0f)] [SerializeField] float KinectMaxNotAvailable = 10.0f;
        [Range(2.0f, 25.0f)] [SerializeField] float TriEdgeThreshold = 10.0f;
        [Range(0.0f, 0.5f)] [SerializeField] float DepthScale = 0.1f;

        private enum MeshState
        {
            StartingUp,
            NoSensor,
            InitialisingMesh,
            AwaitingData,
            RenderingMesh
        }

        // Kinect SDK indicates that downsampling only works at a value of 4 and
        // the recommended operational distances are from the Kinect V2 specs
        private const int downSampleSize = 4;
        private const int depthMinReliableDistance = 500;
        private const int depthMaxReliableDistance = 4500;

        private MeshState currentMeshState;
        private KinectManager kinectManager;
        private Renderer meshRenderer;
        private Mesh mesh;
        private Vector3[] vertices;
        private Vector2[] uv;
        private List<int> thresholdTris;

        private TextMeshProUGUI meshStatusText;
        private string meshStatusDefaultText;
        private float kinectAvailableTimer;

        void Start()
        {
            // Retrieve the data manager component and a reference to the mesh renderer
            kinectManager = gameObject.GetComponent<KinectManager>();
            meshRenderer = gameObject.GetComponent<Renderer>();

            // Store the a reference to the mesh status text and its default value. Then, show
            // the view status message and hide the mesh renderer until the mesh is ready to display
            meshStatusText = MeshStatusTextContainer.GetComponentInChildren<TextMeshProUGUI>();
            meshStatusDefaultText = meshStatusText.text;
            MeshStatusTextContainer.SetActive(true);
            meshRenderer.enabled = false;

            // Proceed with initialisation if the sensor reference exists
            if (kinectManager.DoesSensorExist())
            {
                // Set the mesh state to initialising mesh
                SetMeshState(MeshState.InitialisingMesh);

                // Initialise an empty downsampled mesh as a 2D plane with the correct height and width
                // attributes that match the Kinect images
                InitialiseMeshData();

                // Align the centre of the mesh to its parent container origin, which by default is world
                // space
                AlignMeshToWorldOrigin();
            }
            else
            {
                // Otherwise, set the mesh state to the no sensor state
                SetMeshState(MeshState.NoSensor);
            }

            kinectAvailableTimer = 0.0f;
        }

        void Update()
        {
            // If the sensor reference does not exist, set the mesh state to no sensor
            if (!kinectManager.DoesSensorExist())
            {
                SetMeshState(MeshState.NoSensor);
                return;
            }
            else if (!kinectManager.IsSensorReady())
            {
                // If the sensor is not yet available, first check for how long it hasn't
                // been available
                if (kinectAvailableTimer > KinectMaxNotAvailable)
                {
                    // If it has been unavailable longer than the max, set the state to
                    // no sensor and return whilst it is still not available
                    SetMeshState(MeshState.NoSensor);
                    return;
                }

                // Otherwise, if the max not available time has not yet been reached, set
                // the mesh state to awaiting data and keep adding to the timer
                SetMeshState(MeshState.AwaitingData);
                kinectAvailableTimer += Time.deltaTime;
                return;
            }
            else
            {
                // Otherwise, if the kinect available timer has been counting for some
                // time but the sensor is now available, reset it back to 0
                if (kinectAvailableTimer != 0.0f)
                {
                    kinectAvailableTimer = 0.0f;
                }
            }

            // If the conditions above pass, use the kinect manager to identify if it is receiving data
            // from the hardware yet or not
            bool dataAvailable = kinectManager.IsDataAvailable();
            if (!dataAvailable)
            {
                // If not, change the state to indicate that it is awaiting data if it hasn't already
                // been set and return
                SetMeshState(MeshState.AwaitingData);
                return;
            }
            else if (!meshRenderer.enabled)
            {
                // Otherwise, if it is receiving data and the renderer is not enabled, the state has just
                // changed, so reflect this in the system
                SetMeshState(MeshState.RenderingMesh);
            }

            // Retrieve the current color texture from the data manager and assign it to the main texture
            // of the mesh material. Then, refresh the mesh data with the new depth data to reflect changes
            // in the scene
            meshRenderer.material.mainTexture = kinectManager.ColourTexture;
            RefreshDepthData();
        }

        internal Vector3[] GetMeshVertices()
        {
            return vertices;
        }

        internal Vector2[] GetMeshUvs()
        {
            return uv;
        }

        private void InitialiseMeshData()
        {
            // Use downsampled width and height to lower the resolution and reduce data size for processing 
            // and storage
            int meshWidth = kinectManager.DepthFrameWidth / downSampleSize;
            int meshHeight = kinectManager.DepthFrameHeight / downSampleSize;

            // Create a new mesh object and set it as the component for the game object
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;

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

        private void AlignMeshToWorldOrigin()
        {
            // Based on the Kinect parameters, depth scale and downsampling value, we can
            // first establish the centre of the mesh in in its local space
            Vector3 meshLocalCentre = new Vector3(0, 0, 0)
            {
                x = (kinectManager.DepthFrameWidth / downSampleSize) / 2,
                y = -(kinectManager.DepthFrameHeight / downSampleSize) / 2,
                z = (depthMaxReliableDistance - depthMinReliableDistance) * DepthScale / 2
            };

            // Then, we can adjust for the minimum reliable distance that the Kinect can gather
            // data from, as the mesh is slightly offset from its actual origin by this amount
            meshLocalCentre += new Vector3(0, 0, depthMinReliableDistance * DepthScale);

            // Finally, we can move the game object origin back to the world origin and then
            // align its centre with the world origin by adding the local centre vector to this
            // translation
            gameObject.transform.position -= (gameObject.transform.position + meshLocalCentre);

            // TODO : Could also use bounds to prevent the camera from getting to close to the mesh?
        }

        private void RefreshDepthData()
        {
            // Get the new depth data from the manager
            ushort[] depthData = kinectManager.DepthData;

            // Initialise an array of colour space points that is the same size as the
            // incoming depth data array
            ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];

            // Map the depth frame coordinates to colour space so that the the colorSpace
            // array is filled with ColorSpacePoints that correspond to each depth value,
            // consequently aligning the two images
            kinectManager.Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);

            // Initialise an empty triangles list to dynamically store the valid triangles
            // for rendering the mesh
            thresholdTris = new List<int>();

            // For each set of downsampled pixel rows:
            for (int y = 0; y < kinectManager.DepthFrameHeight; y += downSampleSize)
            {
                // Define a vector3 array for storing quad corners and a float array for storing
                // quad edges
                Vector3[] quadCorners = new Vector3[4];
                float[] quadEdges = new float[5];

                // Then, for each set of downsampled pixels across each row:
                for (int x = 0; x < kinectManager.DepthFrameWidth; x += downSampleSize)
                {
                    // Get the downsampled x and y indices and then get the downsampled array index
                    // using these values
                    int indexX = x / downSampleSize;
                    int indexY = y / downSampleSize;
                    int smallIndex = (indexY * (kinectManager.DepthFrameWidth / downSampleSize)) + indexX;

                    // Find the average value of the actual depth points within the current downsampling
                    // region to get a single average depth value. Then, scale this value down to reduce the
                    // size of the mesh in the scene view and assign the current value to the Z parameter of
                    // the next vertex to set its position in the mesh
                    double avg = GetAvg(depthData, x, y, kinectManager.DepthFrameWidth);
                    avg *= DepthScale;
                    vertices[smallIndex].z = (float)avg;

                    // Update the UV mapping by finding the corresponding ColorSpacePoint for the current
                    // downsampled pixel and adding its 2D normalised coordinates at the same index position
                    ColorSpacePoint colorSpacePoint = colorSpace[(y * kinectManager.DepthFrameWidth) + x];
                    uv[smallIndex] = new Vector2(colorSpacePoint.X / kinectManager.ColourFrameWidth, colorSpacePoint.Y / kinectManager.ColourFrameHeight);

                    // Define triangles if the height pixel index is past the first row of vertices and the row
                    // pixel index is on at least the second vertex of the current row
                    if (indexY >= 1 && indexX >= 1)
                    {
                        // Get the small index of the top left corner of the quad, as the actual small index
                        // is currently on the bottom right
                        int triTopLeftIndex = smallIndex - 1 - (kinectManager.DepthFrameWidth / downSampleSize);

                        // Get each of the vectors that make up the corners of the quad
                        quadCorners[0] = vertices[triTopLeftIndex]; // top left
                        quadCorners[1] = vertices[triTopLeftIndex + 1]; // top right
                        quadCorners[2] = vertices[smallIndex - 1]; // bottom left
                        quadCorners[3] = vertices[smallIndex]; // bottom right

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
                            thresholdTris.Add(smallIndex - 1);
                        }

                        // Do the same for the second triangle in the quad
                        if (quadEdges[1] <= TriEdgeThreshold && quadEdges[3] <= TriEdgeThreshold && quadEdges[4] <= TriEdgeThreshold)
                        {
                            // Add triangle using bottom left corner index, top right and bottom right
                            thresholdTris.Add(smallIndex - 1);
                            thresholdTris.Add(triTopLeftIndex + 1);
                            thresholdTris.Add(smallIndex);
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

        private double GetAvg(ushort[] depthData, int x, int y, int width)
        {
            double sum = 0.0;

            // Iterate through all of the original depth points in the current
            // downsampling region
            for (int y1 = y; y1 < y + downSampleSize; y1++)
            {
                for (int x1 = x; x1 < x + downSampleSize; x1++)
                {
                    // Get the next non-downsampled index position
                    int fullIndex = (y1 * width) + x1;

                    // If the depth value at the current position is 0, add the
                    // maximum reliable depth value to the running total of values
                    // for this sample
                    if (depthData[fullIndex] == 0)
                    {
                        sum += depthMaxReliableDistance; // TODO : Experiment with changing this
                    }
                    else
                    {
                        // Otherwise, add the actual depth value
                        sum += depthData[fullIndex];
                    }

                }
            }

            // Return an average of all of the depth values to get a single
            // downsampled value for the given region
            return sum / (downSampleSize * downSampleSize);
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
                // For the no sensor state, show the placeholder plane, then set the mesh status text and
                // display it if not already visible
                case MeshState.NoSensor:
                    meshStatusText.text = NoSensorText;
                    meshStatusText.color = Color.red;
                    if (!MeshStatusTextContainer.activeSelf) { MeshStatusTextContainer.SetActive(true); }
                    break;
                // For the initialising mesh and awaiting data states, show the placeholder plane again, then
                // set the mesh status message text back to its default and display its container if not already
                // visible
                case MeshState.InitialisingMesh:
                case MeshState.AwaitingData:
                    meshStatusText.text = meshStatusDefaultText;
                    if (!MeshStatusTextContainer.activeSelf) { MeshStatusTextContainer.SetActive(true); }
                    break;
                // For the rendering mesh state, hide the placeholder plane and the mesh status text if visible,
                // then enable the mesh renderer if not visible
                case MeshState.RenderingMesh:
                    if (MeshStatusTextContainer.activeSelf) { MeshStatusTextContainer.SetActive(false); }
                    if (!meshRenderer.enabled) { meshRenderer.enabled = true; }
                    break;
            }

            // Store the new state
            currentMeshState = newState;
        }
    }
}
