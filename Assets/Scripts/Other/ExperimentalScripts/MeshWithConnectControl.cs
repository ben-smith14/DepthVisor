using UnityEngine;
using Windows.Kinect;
using System.Collections.Generic;
using TMPro;

public class MeshWithConnectControl : MonoBehaviour
{
    [SerializeField] GameObject MultiSourceManager;
    [SerializeField] GameObject SensorMessage;
    [Range(2.0f, 25.0f)] [SerializeField] float EdgeThreshold = 10.0f;

    private KinectSensor sensor;
    private CoordinateMapper mapper;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uv;
    private int[] triangles;
    private List<int> thresholdTris;
    private bool kinectAvailable;

    // Only works at 4 right now (indicated by Kinect SDK)
    private const int downsampleSize = 4;
    private const double depthScale = 0.1f;
    private const int speed = 50;
    
    // From Microsoft docs
    private const int depthMinReliableDistance = 500;
    private const int depthMaxReliableDistance = 4500;

    private MultiSourceManager multiManager;
    private bool recordFrames;

    void Start()
    {
        // Get the main Kinect sensor and add a subscription method to the IsAvailableChanged event
        // in the KinectSensor class to trigger the subscribed method when the availability of the
        // sensor changes
        sensor = KinectSensor.GetDefault();
        sensor.IsAvailableChanged += Sensor_IsAvailableChanged;
        kinectAvailable = false;

        // Show the sensor message
        SensorMessage.SetActive(true);
    }

    void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
    {
        Debug.Log(sensor.IsAvailable);

        if (sensor.IsAvailable)
        {
            // If the sensor has just become available, hide the message text and flip
            // the available flag
            SensorMessage.SetActive(false);
            kinectAvailable = true;
        }
        else
        {
            // Otherwise, set the sensor message to show that no device could be found, make it
            // visible and flip the available flag once again
            SensorMessage.SetActive(true);
            kinectAvailable = false;
        }
    }

    void Update()
    {
        // Get the visibility of the mesh renderer
        bool visibleMesh = gameObject.GetComponent<MeshRenderer>().enabled;

        // If the kinect is not available and the mesh renderer is hidden, simply
        // return, as the connection state has not changed and there is nothing to
        // be done
        if (!kinectAvailable && !visibleMesh)
        {
            return;
        }

        // If the kinect is not available but the mesh renderer is visible, the connection
        // has only just ended, so close the connection and return
        if (!kinectAvailable)
        {
            CloseConnection();
            return;
        }

        // If the kinect is available but the mesh renderer is hidden, the connection has
        // only just been established, so initialise it and return
        if (kinectAvailable && !visibleMesh)
        {
            InitialiseConnection();
            return;
        }

        // Cache the reference to the multi-source manager
        multiManager = MultiSourceManager.GetComponent<MultiSourceManager>();

        // Otherwise, the kinect is available and the mesh renderer is already visible, so the
        // connection state has not changed and we have an existing mesh to update. Start by
        // retrieving the current color texture from the multi source manager
        Texture2D colorView = multiManager.GetColorTexture();

        // Assign the main texture of the mesh material to this texture and refresh the
        // mesh data to reflect changes to the scene
        gameObject.GetComponent<Renderer>().material.mainTexture = colorView;
        RefreshData(multiManager.GetDepthData(), multiManager.ColorWidth, multiManager.ColorHeight);

        // Save the data
        //if (recordFrames)
        //{
        //    RecordCurrentFrame(mesh.vertices, mesh.uv, colorView);
        //}
    }

    private void CloseConnection()
    {
        // Deactivate the mesh renderer to hide the mesh that is no longer
        // updating from the user
        gameObject.GetComponent<MeshRenderer>().enabled = false;

        // Dereference the CoordinateMapper and close the sensor if open
        if (mapper != null)
        {
            mapper = null;
        }
    }

    private void InitialiseConnection()
    {
        // Cache the CoordinateMapper reference using the sensor
        mapper = sensor.CoordinateMapper;

        // Get the depth frame information and initialise the mesh, which has been downsampled
        // to reduce the amount of information to store
        var frameDesc = sensor.DepthFrameSource.FrameDescription;
        InitialiseMesh(frameDesc.Width / downsampleSize, frameDesc.Height / downsampleSize);

        // Make sure that the mesh renderer is activated so that the mesh is visible
        gameObject.GetComponent<MeshRenderer>().enabled = true;

        // Ensure that the sensor is open
        if (!sensor.IsOpen)
        {
            sensor.Open();
        }
    }

    void InitialiseMesh(int width, int height)
    {
        // Create a new mesh object and set it as the component for the game object
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Initialise the data structure arrays for the mesh
        vertices = new Vector3[width * height];
        uv = new Vector2[width * height];
        triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Get the single index for the arrays, which starts in the top left of
                // incoming images and sweeps across each row from left to right, wrapping
                // around to subsequent rows as it goes downwards
                int index = (y * width) + x;

                // Fill the vertex and uv arrays, inverting the y values in world space to display
                // the mesh in the correct orientation
                vertices[index] = new Vector3(x, -y, 0);
                uv[index] = new Vector2((x / width), (y / height));

                // Skip the last row and column, as there will be no outer points to
                // include in the quads at these vertices
                if (x != (width - 1) && y != (height - 1))
                {
                    // Add all triangles to the mesh for now by assigning the indices of the
                    // vertices that make up the corners
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    // Triangle 1 of the current quad
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;

                    // Triangle 2 of the current quad
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        // Load in the new data to the mesh object and recalculate normals to ensure lighting
        // and shading is correct
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private void RefreshData(ushort[] depthData, int colorWidth, int colorHeight)
    {
        // Get the current frame description and initialise an array of colour space
        // points that is the same size as the depth data array
        var frameDesc = sensor.DepthFrameSource.FrameDescription;
        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];

        // Map the depth frame coordinates to colour space so that the the colorSpace
        // array is filled with ColorSpacePoints that correspond to each depth value,
        // consequently aligning the two images
        mapper.MapDepthFrameToColorSpace(depthData, colorSpace);

        // Initialise an empty triangles list to dynamically store the valid triangles
        // for rendering the mesh
        thresholdTris = new List<int>();

        // For each group of pixel rows to downsample for the mesh
        for (int y = 0; y < frameDesc.Height; y += downsampleSize)
        {
            // Define a vector3 array for storing quad corners and a float array for storing
            // quad edges
            Vector3[] quadCorners = new Vector3[4];
            float[] quadEdges = new float[5];

            // Then, for each group of pixels across each row
            for (int x = 0; x < frameDesc.Width; x += downsampleSize)
            {
                // Get the downsampled x and y indices and then get the downsampled array index
                // using these values
                int indexX = x / downsampleSize;
                int indexY = y / downsampleSize;
                int smallIndex = (indexY * (frameDesc.Width / downsampleSize)) + indexX;

                // Find the average value of the actual depth points within the current downsampled
                // region to get a single average depth value. Then scale this value and assign it
                // to the Z parameter of the next vertex to set its position in the mesh
                double avg = GetAvg(depthData, x, y, frameDesc.Width);
                avg *= depthScale;
                vertices[smallIndex].z = (float) avg;
                
                // Update the UV mapping by finding the corresponding ColorSpacePoint for the downsampled
                // depth data index positions and assiging its normalised coordinates to the map
                var colorSpacePoint = colorSpace[(y * frameDesc.Width) + x];
                uv[smallIndex] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);

                // Define triangles if the height pixel index is past the first row of vertices and the row
                // pixel index is on at least the second vertex of the current row
                if (indexY >= 1 && indexX >= 1)
                {
                    // Get the small index of the top left corner of the quad, as the actual small index
                    // is currently on the bottom right
                    int triTopLeftIndex = smallIndex - 1 - (frameDesc.Width / downsampleSize);

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

        // Clear the mesh and then load in the new data to the mesh, converting the triangle list to
        // an array
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
        // downsampled value
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

    // Clean up the Kinect data structures on application quit
    void OnApplicationQuit()
    {
        if (mapper != null)
        {
            mapper = null;
        }
        
        if (sensor != null)
        {
            if (sensor.IsOpen)
            {
                sensor.Close();
            }

            sensor = null;
        }
    }
}
