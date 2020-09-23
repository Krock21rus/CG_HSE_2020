using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public MetaBallField Field = new MetaBallField();
    
    private MeshFilter _filter;
    private Mesh _mesh;
    
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<int> indices = new List<int>();

    /// <summary>
    /// Executed by Unity upon object initialization. <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// </summary>
    private void Awake()
    {
        // Getting a component, responsible for storing the mesh
        _filter = GetComponent<MeshFilter>();
        
        // instantiating the mesh
        _mesh = _filter.mesh = new Mesh();
        
        // Just a little optimization, telling unity that the mesh is going to be updated frequently
        _mesh.MarkDynamic();
    }

    /// <summary>
    /// Executed by Unity on every frame <see cref="https://docs.unity3d.com/Manual/ExecutionOrder.html"/>
    /// You can use it to animate something in runtime.
    /// </summary>
    private void Update()
    {
        List<Vector3> cubeVertices = new List<Vector3>
        {
            new Vector3(0, 0, 0), // 0
            new Vector3(0, 1, 0), // 1
            new Vector3(1, 1, 0), // 2
            new Vector3(1, 0, 0), // 3
            new Vector3(0, 0, 1), // 4
            new Vector3(0, 1, 1), // 5
            new Vector3(1, 1, 1), // 6
            new Vector3(1, 0, 1), // 7
        };

        int[] sourceTriangles =
        {
            0, 1, 2, 2, 3, 0, // front
            3, 2, 6, 6, 7, 3, // right
            7, 6, 5, 5, 4, 7, // back
            0, 4, 5, 5, 1, 0, // left
            0, 3, 7, 7, 4, 0, // bottom
            1, 5, 6, 6, 2, 1, // top
        };

        
        vertices.Clear();
        indices.Clear();
        normals.Clear();
        
        Field.Update();
        // ----------------------------------------------------------------
        // Generate mesh here. Below is a sample code of a cube generation.
        // ----------------------------------------------------------------

        float BOX_RANGE = 4f;
        float STEP = 0.08f;
        float D = 0.0001f;

        for (float i1 = -BOX_RANGE; i1 <= BOX_RANGE; i1 += STEP)
        {
            for (float i2 = -BOX_RANGE; i2 <= BOX_RANGE; i2 += STEP)
            {
                for (float i3 = -BOX_RANGE; i3 <= BOX_RANGE; i3 += STEP)
                {
                    List<Vector3> cubeVertices2 = new List<Vector3>
                    {
                        new Vector3(i1, i2, i3), // 0
                        new Vector3(i1, i2 + STEP, i3), // 1
                        new Vector3(i1 + STEP, i2 + STEP, i3), // 2
                        new Vector3(i1 + STEP, i2, i3), // 3
                        new Vector3(i1, i2, i3 + STEP), // 4
                        new Vector3(i1, i2 + STEP, i3 + STEP), // 5
                        new Vector3(i1 + STEP, i2 + STEP, i3 + STEP), // 6
                        new Vector3(i1 + STEP, i2, i3 + STEP), // 7
                    };
                    int mask = 0;
                    int curpow2 = 1;
                    for (int j = 0; j < 8; j++)
                    {
                        float val = Field.F(cubeVertices2[j]);
                        if (val > 0)
                        {
                            mask += curpow2;
                        }
                        curpow2 *= 2;
                    }
                    for (int j = 0; j < MarchingCubes.Tables.CaseToTrianglesCount[mask]; j++)
                    {
                        var arr = MarchingCubes.Tables.CaseToVertices[mask][j];
                        List<int> arrvals = new List<int>
                    {
                        arr[0],
                        arr[1],
                        arr[2]
                    };
                        foreach (int val in arrvals)
                        {
                            if (val == -1)
                            {
                                break;
                            }
                            int[] edge = MarchingCubes.Tables._cubeEdges[val];
                            float fval1 = Field.F(cubeVertices2[edge[0]]);
                            float fval2 = Field.F(cubeVertices2[edge[1]]);
                            float len = Math.Abs(fval2 - fval1);
                            Vector3 vertexPos = cubeVertices2[edge[0]] + (cubeVertices2[edge[1]] - cubeVertices2[edge[0]]) * ((Math.Abs(fval1)) / len);
                            indices.Add(vertices.Count);
                            vertices.Add(vertexPos);

                            float x = Field.F(vertexPos - new Vector3(D, 0, 0)) - Field.F(vertexPos + new Vector3(D, 0, 0));
                            float y = Field.F(vertexPos - new Vector3(0, D, 0)) - Field.F(vertexPos + new Vector3(0, D, 0));
                            float z = Field.F(vertexPos - new Vector3(0, 0, D)) - Field.F(vertexPos + new Vector3(0, 0, D));
                            normals.Add(new Vector3(x, y, z));
                        }
                    }
                }
            }
        }
        // Here unity automatically assumes that vertices are points and hence (x, y, z) will be represented as (x, y, z, 1) in homogenous coordinates
        _mesh.Clear();
        Debug.Log(vertices.Count);
        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(indices, 0);
        _mesh.SetNormals(normals); // Use _mesh.SetNormals(normals) instead when you calculate them

        // Upload mesh data to the GPU
        _mesh.UploadMeshData(false);
    }
}