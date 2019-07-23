using UnityEngine;
using Windows.Kinect;
using System.Collections.Generic;

public class KinectMeshGenerator : MonoBehaviour
{
    [SerializeField] GameObject MultiSourceManager;
    [Range(2.0f, 25.0f)] [SerializeField] float EdgeThreshold = 10.0f;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;
    private List<int> _ThresholdTris;

    // Only works at 4 right now (indicated by Kinect SDK)
    private const int _DownsampleSize = 4;
    private const double _DepthScale = 0.1f;
    private const int _Speed = 50;
    
    // From Microsoft docs
    private const int _DepthMinReliableDistance = 500;
    private const int _DepthMaxReliableDistance = 4500;

    private MultiSourceManager _MultiManager;

    // Initialise the connection to the Kinect
    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            // Downsample mesh to lower resolution
            CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }

    void CreateMesh(int width, int height)
    {
        // Create a new mesh object and set it as the component for the game object
        _Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _Mesh;

        // Initialise the data structure arrays for the mesh
        _Vertices = new Vector3[width * height];
        _UV = new Vector2[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Get the single index for the arrays, which starts in the top left of
                // incoming images and sweeps across each row from left to right, wrapping
                // around to subsequent rows as it goes downwards
                int index = (y * width) + x;

                // TODO: WHAT HAPPENS IF I REMOVE THE NEGATIVE SIGN??
                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2((x / width), (y / height));

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
                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;

                    // Triangle 2 of the current quad
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        // Load in the new data to the mesh object and recalculate normals to ensure lighting
        // and shading is correct
        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }

    void Update()
    {
        // If sensor is disconnected, return
        if (_Sensor == null)
        {
            return;
        }

        // If the multi source manager is not defined, return
        if (MultiSourceManager == null)
        {
            return;
        }
        
        // Otherwise, try and retrieve the multi manager component of the object
        // and ensure that it has also been set
        _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
        if (_MultiManager == null)
        {
            return;
        }

        // Assign the main texture of the mesh material to the current colour image from the
        // multi source manager and refresh the mesh data to reflect changes to the scene
        gameObject.GetComponent<Renderer>().material.mainTexture = _MultiManager.GetColorTexture();
        RefreshData(_MultiManager.GetDepthData(), _MultiManager.ColorWidth, _MultiManager.ColorHeight);
    }
    
    private void RefreshData(ushort[] depthData, int colorWidth, int colorHeight)
    {
        // Get the current frame description and initialise an array of colour space
        // points that is the same size as the depth data array
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;
        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];

        // Map the depth frame coordinates to colour space so that the the colorSpace
        // array is filled with ColorSpacePoints that correspond to each depth value,
        // consequently aligning the two images
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);

        // Initialise the triangles list to dynamically store the valid triangles
        // for rendering the mesh
        _ThresholdTris = new List<int>();

        // For each row
        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        {
            // Define a vector3 array for storing quad corners and a float array for storing
            // quad edges
            Vector3[] quadCorners = new Vector3[4];
            float[] quadEdges = new float[5];

            // For each vertex in each row
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
            {
                // Get the downsampled x and y indices and then get the downsampled array index
                // using these values
                int indexX = x / _DownsampleSize;
                int indexY = y / _DownsampleSize;
                int smallIndex = (indexY * (frameDesc.Width / _DownsampleSize)) + indexX;

                // Find the average value of the actual depth points within a downsampled region
                // to get a single depth value. Then scale this value and assign it to the Z
                // parameter of the next downsampled vertex to move the vertex in the mesh
                double avg = GetAvg(depthData, x, y, frameDesc.Width);
                avg *= _DepthScale;
                _Vertices[smallIndex].z = (float) avg;
                
                // Update the UV mapping by finding the corresponding ColorSpacePoint for the current
                // array index position and assiging its normalised pixel coordinates to the map
                var colorSpacePoint = colorSpace[(y * frameDesc.Width) + x];
                _UV[smallIndex] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);

                // Define triangles if the height index is past the first row of vertices and the row index
                // is on at least the second vertex of the current row
                if (indexY >= 1 && indexX >= 1)
                {
                    // Get the small index of the top left corner of the quad, as the actual small index
                    // is currently on the bottom right
                    int triTopLeftIndex = smallIndex - 1 - (frameDesc.Width / _DownsampleSize);

                    // Get each of the vectors that make up the corners of the quad
                    quadCorners[0] = _Vertices[triTopLeftIndex]; // top left
                    quadCorners[1] = _Vertices[triTopLeftIndex + 1]; // top right
                    quadCorners[2] = _Vertices[smallIndex - 1]; // bottom left
                    quadCorners[3] = _Vertices[smallIndex]; // bottom right

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
                        _ThresholdTris.Add(triTopLeftIndex);
                        _ThresholdTris.Add(triTopLeftIndex + 1);
                        _ThresholdTris.Add(smallIndex - 1);
                    }

                    // Do the same for the second triangle in the quad
                    if (quadEdges[1] <= EdgeThreshold && quadEdges[3] <= EdgeThreshold && quadEdges[4] <= EdgeThreshold)
                    {
                        // Add triangle using bottom left corner index, top right and bottom right
                        _ThresholdTris.Add(smallIndex - 1);
                        _ThresholdTris.Add(triTopLeftIndex + 1);
                        _ThresholdTris.Add(smallIndex);
                    }
                }
            }
        }

        // Load in the new data to the mesh object again
        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _ThresholdTris.ToArray();
        _Mesh.RecalculateNormals();

        // BACKUP TRIANGLES DEFINITION - CAN DELETE IF OTHER ONE WORKS
        //for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        //{
        //    for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
        //    {
        //        int indexX = x / _DownsampleSize;
        //        int indexY = y / _DownsampleSize;
        //        int smallIndex = (indexY * (frameDesc.Width / _DownsampleSize)) + indexX;

        //        // Skip the last row/col
        //        if (x != (frameDesc.Width - _DownsampleSize) && y != (frameDesc.Height - _DownsampleSize))
        //        {
        //            // Set up triangle corners
        //            int topLeft = smallIndex;
        //            int topRight = topLeft + 1;
        //            int bottomLeft = topLeft + (frameDesc.Width / _DownsampleSize);
        //            int bottomRight = bottomLeft + 1;

        //            // Find the vertex coordinates for each one
        //            Vector3 vertex1 = _Vertices[topLeft];
        //            Vector3 vertex2 = _Vertices[topRight];
        //            Vector3 vertex3 = _Vertices[bottomLeft];
        //            Vector3 vertex4 = _Vertices[bottomRight];

        //            // Find the length of all edges in the quad
        //            float edge1 = Vector3.Distance(vertex1, vertex2);
        //            float edge2 = Vector3.Distance(vertex2, vertex3);
        //            float edge3 = Vector3.Distance(vertex3, vertex1);
        //            float edge4 = Vector3.Distance(vertex2, vertex4);
        //            float edge5 = Vector3.Distance(vertex4, vertex3);

        //            // If all edges of the first triangle are less than or equal to the threshold
        //            // value, add the triangle
        //            if (edge1 <= EdgeThreshold && edge2 <= EdgeThreshold && edge3 <= EdgeThreshold)
        //            {
        //                _ThresholdTris.Add(topLeft);
        //                _ThresholdTris.Add(topRight);
        //                _ThresholdTris.Add(bottomLeft);
        //            }

        //            // Do the same for the second triangle in the quad
        //            if (edge2 <= EdgeThreshold && edge4 <= EdgeThreshold && edge5 <= EdgeThreshold)
        //            {
        //                _ThresholdTris.Add(bottomLeft);
        //                _ThresholdTris.Add(topRight);
        //                _ThresholdTris.Add(bottomRight);
        //            }
        //        }
        //    }
        //}
    }
    
    private double GetAvg(ushort[] depthData, int x, int y, int width)
    {
        double sum = 0.0;

        // Iterate through all of the original depth points in the current
        // downsampling region
        for (int y1 = y; y1 < y + _DownsampleSize; y1++)
        {
            for (int x1 = x; x1 < x + _DownsampleSize; x1++)
            {
                // Get the next non-downsampled index position
                int fullIndex = (y1 * width) + x1;

                // TODO : WHAT HAPPENS IF I ADD 0 INSTEAD OF MAX RELIABLE?
                // TODO : WHAT ALSO HAPPENS IF I CLAMP THE DEPTH VALUES TO BETWEEN MIN AND MAX?
                // TODO : HOW DO I KNOW IF A 0 VALUE REPRESENTS TOO CLOSE OR TOO FAR?
                // TODO : IS THERE A BETTER WAY TO HANDLE TAKING AN AVERAGE HERE?

                // If the depth value at the current position is 0, add the
                // maximum reliable depth value to the running total of values
                // for this sample
                if (depthData[fullIndex] == 0)
                {
                    sum += _DepthMaxReliableDistance;
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
        return sum / (_DownsampleSize * _DownsampleSize);
    }

    // Deference the Kinect data structures on quit
    void OnApplicationQuit()
    {
        if (_Mapper != null)
        {
            _Mapper = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
