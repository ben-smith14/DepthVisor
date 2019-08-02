using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

// TODO : TEST REMOVAL OF TRIANGLE GENERATION FROM START METHOD

namespace DepthVisor.Kinect
{
    public class KinectMeshGenerator : MonoBehaviour
    {
        [SerializeField] GameObject SensorMessage;
        [Range(2.0f, 25.0f)] [SerializeField] float EdgeThreshold = 10.0f;

        private KinectManager kinectManager;
        private Mesh mesh;
        private Vector3[] vertices;
        private Vector2[] uv;
        private List<int> thresholdTris;

        // Kinect SDK indicates that downsampling only works at a value of 4 and
        // the recommended operational distances are from the Kinect V2 specs
        private const int downsampleSize = 4;
        private const double depthScale = 0.1f; // TODO : WHAT HAPPENS IF I REMOVE THIS?
        private const int depthMinReliableDistance = 500;
        private const int depthMaxReliableDistance = 4500;

        private bool recordFrames;

        void Start()
        {
            // Retrieve the data manager component
            kinectManager = gameObject.GetComponent<KinectManager>();

            // Proceed with initialisation if the sensor reference is valid
            if (!kinectManager.IsSensorNull())
            {
                // Hide the no sensor message
                SensorMessage.SetActive(false);

                // Initialise a downsampled mesh to lower the resolution
                InitialiseMesh(kinectManager.DepthFrameWidth / downsampleSize, kinectManager.DepthFrameHeight / downsampleSize);

                // Add the event handler that will update the mesh with new data from the Kinect
                // to the event in the data manager class that is triggered when new data arrives
                kinectManager.NewDataArrived += UpdateNewMeshData;
            } else
            {
                // Otherwise, show the no sensor message
                SensorMessage.SetActive(true);
            }
        }

        void InitialiseMesh(int meshWidth, int meshHeight)
        {
            // Create a new mesh object and set it as the component for the game object
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;

            // Initialise the data structure arrays for the mesh
            vertices = new Vector3[meshWidth * meshHeight];
            uv = new Vector2[meshWidth * meshHeight];

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
                }
            }

            // Load in the new data to the mesh object and recalculate normals to ensure lighting
            // and shading is correct
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.RecalculateNormals();

            // TODO : PROBABLY NEED TO DO THIS AT LEAST ONCE IN REFRESH DATA AS WELL

            // TODO : Use bounds centre to align the mesh to centre of world space?
            Vector3 meshCentre = mesh.bounds.center;
            gameObject.transform.position = -meshCentre;

            // also use bounds extent to align bottom of the mesh with the floor?
            Vector3 meshExtents = mesh.bounds.extents;
            gameObject.transform.position += new Vector3(0, meshExtents.y, 0);

            // could also use bounds to prevent the camera from getting to close to the mesh?
        }

        private void UpdateNewMeshData(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            // If the sensor is disconnected or there is no reference to a CoordinateMapper,
            // return and display the no sensor message
            if (kinectManager.IsSensorNull() || kinectManager.Mapper == null)
            {
                SensorMessage.SetActive(true);
                return;
            }

            // Otherwise, retrieve the current color texture from the data manager and assign it
            // to the main texture of the mesh material. Then, refresh the mesh data with the new
            // depth data to reflect changes in the scene
            gameObject.GetComponent<Renderer>().material.mainTexture = kinectManager.ColourTexture;
            RefreshDepthData(kinectManager.DepthData);

            // Save the current data frames (MIGHT NEED TO DO IN REFRESH SO THAT THE
            // IMAGES ARE ALIGNED)
            //if (recordFrames)
            //{
            //    RecordCurrentFrame(mesh.vertices, mesh.uv, colorView);
            //}
        }

        private void RefreshDepthData(ushort[] depthData)
        {
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
            for (int y = 0; y < kinectManager.DepthFrameHeight; y += downsampleSize)
            {
                // Define a vector3 array for storing quad corners and a float array for storing
                // quad edges
                Vector3[] quadCorners = new Vector3[4];
                float[] quadEdges = new float[5];

                // Then, for each set of downsampled pixels across each row:
                for (int x = 0; x < kinectManager.DepthFrameWidth; x += downsampleSize)
                {
                    // Get the downsampled x and y indices and then get the downsampled array index
                    // using these values
                    int indexX = x / downsampleSize;
                    int indexY = y / downsampleSize;
                    int smallIndex = (indexY * (kinectManager.DepthFrameWidth / downsampleSize)) + indexX;

                    // Find the average value of the actual depth points within the current downsampling
                    // region to get a single average depth value. Then scale this value and assign it
                    // to the Z parameter of the next vertex to set its position in the mesh
                    double avg = GetAvg(depthData, x, y, kinectManager.DepthFrameWidth);
                    avg *= depthScale; // TODO : WHAT IF I REMOVE THIS?
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
                        int triTopLeftIndex = smallIndex - 1 - (kinectManager.DepthFrameWidth / downsampleSize);

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
                        // value, add the triangle to the list (define points clockwise to prevent back face culling!)
                        if (quadEdges[0] <= EdgeThreshold && quadEdges[1] <= EdgeThreshold && quadEdges[2] <= EdgeThreshold)
                        {
                            // Add triangle using top left corner index, top right and bottom left
                            thresholdTris.Add(triTopLeftIndex);
                            thresholdTris.Add(triTopLeftIndex + 1);
                            thresholdTris.Add(smallIndex - 1);
                        }

                        // Do the same for the second triangle in the quad
                        if (quadEdges[1] <= EdgeThreshold && quadEdges[3] <= EdgeThreshold && quadEdges[4] <= EdgeThreshold)
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
            // an array of points
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = thresholdTris.ToArray();
            mesh.RecalculateNormals();

            // TODO : Use bounds centre to align the mesh to centre of world space?
            Vector3 meshCentre = mesh.bounds.center;
            gameObject.transform.position = -meshCentre;

            // also use bounds extent to align bottom of the mesh with the floor?
            Vector3 meshExtents = mesh.bounds.extents;
            gameObject.transform.position += new Vector3(0, meshExtents.y, 0);
        }

        private double GetAvg(ushort[] depthData, int x, int y, int width)
        {
            double sum = 0.0;

            // Iterate through all of the original depth points in the current
            // downsampling region
            for (int y1 = y; y1 < y + downsampleSize; y1++)
            {
                for (int x1 = x; x1 < x + downsampleSize; x1++)
                {
                    // Get the next non-downsampled index position
                    int fullIndex = (y1 * width) + x1;

                    // If the depth value at the current position is 0, add the
                    // maximum reliable depth value to the running total of values
                    // for this sample
                    if (depthData[fullIndex] == 0)
                    {
                        sum += depthMaxReliableDistance;
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
            return sum / (downsampleSize * downsampleSize);
        }

        public void ToggleRecording(bool isRecording)
        {
            // TODO : Toggle recording to match the input and instantiate new singleton of recording object if
            // beginning a new recording.
        }

        private void RecordCurrentFrame(Vector3[] meshVertices, Vector2[] uvMap, Texture2D colorTexture)
        {
            // TODO : Add the input data as a frame to the singleton storage object (use a struct in the recording object class).
        }
    }
}
