using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ensure that game objects of this type always have a MeshFilter
[RequireComponent(typeof(MeshFilter))] 

public class MeshGenerator : MonoBehaviour
{
    // Initialise the mesh object, along with its vertices (3D points)
    // and triangles (lines between points)
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    // Color[] colors;

    // Serialize the size of our mesh grid
    [SerializeField] int xSize = 512;
    [SerializeField] int ySize = 424;

    // Serialize a colour gradient and initialise variables that will limit
    // the terrain height
    //[SerializeField] Gradient gradient;
    //float minTerrainDepth;
    //float maxTerrainDepth;

    // Start is called before the first frame update
    void Start()
    {
        // Create a new mesh object and set the index buffer to 32 bit so that
        // it can hold up to 4 billion vertices
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // Get the MeshFilter component of the game object and assign the new mesh
        // to it
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Call the functions to create the basic components of the mesh (i.e. its
        // vertices and triangles), then update the mesh with these new values
        CreateShape();
        UpdateMesh();
    }

    private void CreateShape()
    {
        /* Create all of the vertices to fill the grid space, where there will
         * be a vertex on each corner of each grid position. If we have a 3 x 2
         * grid for example, we can see that the number of vertices that we will
         * have is the number of grid positions plus 1 on each axis, as we actually
         * have one additional line in each dimension
         * 
         * 3 -------------------
         *   |  4  |  5  |  6  |
         * 2 -------------------
         *   |  1  |  2  |  3  |
         * 1 -------------------
         *   1     2     3     4
         *   
         * Therefore, the formula is as can be seen below:
         */
        vertices = new Vector3[(xSize + 1) * (ySize + 1)];

        // Now populate this vertices array with the coordinates of each vertex in
        // grid. i is the index in this array, which is initialised in the first for
        // loop along with the z grid coordinate
        for (int i = 0, y = 0; y <= ySize; y++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                // To give the grid a random terrain like structure with some depth,
                // add some Perlin Noise to the y component of each vector
                //float z = Mathf.PerlinNoise(x * .3f, y * .3f) * 2f;

                // Each point will have a position in the 2D x/z plane for now, so we
                // will keep the height of all of them (i.e. their y value) as 0 for now
                vertices[i] = new Vector3(x, y, 0f);

                // Set the maximum and minimum terrain height values
                //if (z > maxTerrainDepth)
                //{
                //    maxTerrainDepth = z;
                //}
                //if (z < minTerrainDepth)
                //{
                //    minTerrainDepth = z;
                //}

                // Then increment the index counter
                i++;
            }
        }

        // Define a triangles array that can have 6 points for every square (quad) on
        // the grid
        triangles = new int[xSize * ySize * 6];

        // Initialise vertices and triangle counters
        int vert = 0;
        int tris = 0;

        // For every row in the grid
        for (int y = 0; y < ySize; y++)
        {
            // For every quad in the row
            for (int x = 0; x < xSize; x++)
            {
                /* Each square (or quad) in the grid is made up of two triangles
                 * that consist of 6 edges, so we need to give the vertices that
                 * define these two triangles here. The vert and tris variables
                 * are used to keep track of which quad we are in by offsetting
                 * the index position within the array. Note that that the triangles
                 * need to be defined in a clockwise manner to prevent backface
                 * culling
                 *
                 * The first triangle's initial point is the lowest index vertex
                 * position, which is in its bottom left. As the vertex indices
                 * go from left to right across the x axis, the next point is the
                 * one at its top, which is the row size + 1, as it is the first
                 * vertex in a new row. The final point is then simply the point
                 * next to the first one, which is in the bottom right of the triangle.
                 * It looks like the following:
                 *
                 * Point 2 |\
                 *         | \
                 *         |  \
                 * Point 1 |___\ Point 3
                 */ 

                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;

                /* The second triangle's initial point is the same as the last one
                 * in the first triange, but it is now the only point at its bottom.
                 * The second point is then in the top left and it is once again the
                 * same as the second point in the first triangle. The last point,
                 * which is in the top right, is then simply the point next to point
                 * 2 in the vertices array, so it is the same value with an additional
                 * 1 added on. It looks like the following:

                 *         ____
                 * Point 2 \   | Point 3
                 *          \  |
                 *           \ |
                 *            \| Point 1
                 */

                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                // This forms a square or quad. Finally, we then simply add one onto the
                // starting vertex point counter and add 6 onto the triangle counter, as
                // we have just added 6 points to the array
                vert++;
                tris += 6;
            }

            // Add one onto the vertex counter on the end of each row to keep the counter
            // consitent with the row number
            vert++;
        }

        // If we want to overlay an image onto the mesh, we need to define how it should
        // be displayed at each point. We do this using UVs, which are normalised coordinates
        // mapping to each vertex. For each vertex in the 2D representation of the mesh (so
        // ignoring depth), we calculate the UV coordinate by dividing the unnormalised
        // coordinates by each dimension size and casting to a float
        uvs = new Vector2[vertices.Length];
        for (int i = 0, y = 0; y <= ySize; y++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                uvs[i] = new Vector2((float)x / xSize, (float)y / ySize);
                i++;
            }
        }

        // If we want to set the colour of the mesh depending on the height, we need to define
        // how it should be displayed at each point. We do this using a gradient that is
        // evaluated based on the height value of each vertex
        //colors = new Color[vertices.Length];
        //for (int i = 0, y = 0; y <= ySize; y++)
        //{
        //    for (int x = 0; x <= xSize; x++)
        //    {
        //        // The inverse lerp function is used to normalise the height value so that it is
        //        // between 0 and 1 when it is evaluated
        //        float depth = Mathf.InverseLerp(minTerrainDepth, maxTerrainDepth, vertices[i].z);
        //        colors[i] = gradient.Evaluate(depth);
        //        i++;
        //    }
        //}
    }

    private void UpdateMesh()
    {
        // Clear the existing mesh data
        mesh.Clear();

        // Update the mesh's vertices and triangles
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Update the mesh UVs
        mesh.uv = uvs;

        // Update the mesh colours
        //mesh.colors = colors;

        // Change how the mesh is displayed. This retrieves all of the array indices
        // for the only sub mesh in the mesh object, sets its topology to points and
        // then gives the sub mesh number to modify, which is 0 in this case because
        // there is only one. This generates a point cloud instead of triangle faces
        mesh.SetIndices(mesh.GetIndices(0), MeshTopology.Points, 0);

        // Recalculate the normals to ensure interactions with light are
        // calculated correctly
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    //// Gizmos are used to draw shapes for visual debugging. The internal method
    //// OnDrawGizmos is called with every frame
    //private void OnDrawGizmos()
    //{
    //    // If we have no vertices yet, don't do anything
    //    if (vertices == null)
    //    {
    //        return;
    //    }

    //    // Otherwise, for each vertex, draw a sphere of radius 0.1 units in the
    //    // scene
    //    for (int i = 0; i < vertices.Length; i++)
    //    {
    //        Gizmos.DrawSphere(vertices[i], .1f);
    //    }
    //}

    // Update is called once per frame
    //void Update()
    //{
        
    //}
}
