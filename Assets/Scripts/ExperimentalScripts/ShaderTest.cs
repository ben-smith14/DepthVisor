using UnityEngine;
using Windows.Kinect;
using System.Collections.Generic;

public class ShaderTest : MonoBehaviour
{
    [SerializeField] GameObject MultiSourceManager;
    [Range(2.0f, 25.0f)] [SerializeField] float EdgeThreshold = 5.0f;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;
    private Mesh _Mesh;
    private Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;
    private List<int> _ThresholdTris;

    // Only works at 4 right now - DO I NEED TO DOWNSAMPLE??
    private const int _DownsampleSize = 4;
    private const double _DepthScale = 0.1f;
    private const int _Speed = 50;
    
    private MultiSourceManager _MultiManager;

    // Initialise the connection to the Kinect
    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            // Downsample to lower resolution - DO I NEED TO DO THIS??
            CreateMesh(frameDesc.Width / _DownsampleSize, frameDesc.Height / _DownsampleSize);

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }

    void CreateMesh(int width, int height)
    {
        // Create a new mesh object and set as the component for the game object
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
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2((x / width), (y / height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    // Set up triangle corners
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }

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

        if (MultiSourceManager == null)
        {
            return;
        }
            
        _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
        if (_MultiManager == null)
        {
            return;
        }

        ComputeBuffer depthBuffer = new ComputeBuffer(_Vertices.Length, sizeof(float), ComputeBufferType.Default);
        gameObject.GetComponent<Renderer>().material.mainTexture = _MultiManager.GetColorTexture();

        //RefreshData(_MultiManager.GetDepthData(),
        //            _MultiManager.ColorWidth,
        //            _MultiManager.ColorHeight);
    }
    
    private void RefreshData(ushort[] depthData, int colorWidth, int colorHeight)
    {
        var frameDesc = _Sensor.DepthFrameSource.FrameDescription;
        
        // Fill the colorSpace array with a color value for each pixel in the depth image 
        ColorSpacePoint[] colorSpace = new ColorSpacePoint[depthData.Length];
        _Mapper.MapDepthFrameToColorSpace(depthData, colorSpace);
        
        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
            {
                int indexX = x / _DownsampleSize;
                int indexY = y / _DownsampleSize;
                int smallIndex = (indexY * (frameDesc.Width / _DownsampleSize)) + indexX;
                
                double avg = GetAvg(depthData, x, y, frameDesc.Width, frameDesc.Height);
                
                avg = avg * _DepthScale;
                
                _Vertices[smallIndex].z = (float) avg;
                
                // Update UV mapping with CDRP
                var colorSpacePoint = colorSpace[(y * frameDesc.Width) + x];
                _UV[smallIndex] = new Vector2(colorSpacePoint.X / colorWidth, colorSpacePoint.Y / colorHeight);
            }
        }


        _ThresholdTris = new List<int>();
        for (int y = 0; y < frameDesc.Height; y += _DownsampleSize)
        {
            for (int x = 0; x < frameDesc.Width; x += _DownsampleSize)
            {
                int indexX = x / _DownsampleSize;
                int indexY = y / _DownsampleSize;
                int smallIndex = (indexY * (frameDesc.Width / _DownsampleSize)) + indexX;

                // Skip the last row/col
                if (x != (frameDesc.Width - _DownsampleSize) && y != (frameDesc.Height - _DownsampleSize))
                {
                    // Set up triangle corners
                    int topLeft = smallIndex;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + (frameDesc.Width / _DownsampleSize);
                    int bottomRight = bottomLeft + 1;

                    // Find the vertex coordinates for each one
                    Vector3 vertex1 = _Vertices[topLeft];
                    Vector3 vertex2 = _Vertices[topRight];
                    Vector3 vertex3 = _Vertices[bottomLeft];
                    Vector3 vertex4 = _Vertices[bottomRight];

                    // Find the length of all edges in the quad
                    float edge1 = Vector3.Distance(vertex1, vertex2);
                    float edge2 = Vector3.Distance(vertex2, vertex3);
                    float edge3 = Vector3.Distance(vertex3, vertex1);
                    float edge4 = Vector3.Distance(vertex2, vertex4);
                    float edge5 = Vector3.Distance(vertex4, vertex3);

                    // If all edges of the first triangle are less than or equal to the threshold
                    // value, add the triangle
                    if (edge1 <= EdgeThreshold && edge2 <= EdgeThreshold && edge3 <= EdgeThreshold)
                    {
                        _ThresholdTris.Add(topLeft);
                        _ThresholdTris.Add(topRight);
                        _ThresholdTris.Add(bottomLeft);
                    }

                    // Do the same for the second triangle in the quad
                    if (edge2 <= EdgeThreshold && edge4 <= EdgeThreshold && edge5 <= EdgeThreshold)
                    {
                        _ThresholdTris.Add(bottomLeft);
                        _ThresholdTris.Add(topRight);
                        _ThresholdTris.Add(bottomRight);
                    }
                }
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _ThresholdTris.ToArray();
        _Mesh.RecalculateNormals();
    }
    
    private double GetAvg(ushort[] depthData, int x, int y, int width, int height)
    {
        double sum = 0.0;
        
        for (int y1 = y; y1 < y + 4; y1++)
        {
            for (int x1 = x; x1 < x + 4; x1++)
            {
                int fullIndex = (y1 * width) + x1;
                
                if (depthData[fullIndex] == 0)
                    sum += 4500;
                else
                    sum += depthData[fullIndex];
                
            }
        }

        return sum / 16;
    }

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
