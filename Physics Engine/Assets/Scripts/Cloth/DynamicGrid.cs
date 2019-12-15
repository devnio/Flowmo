using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DynamicGrid : MonoBehaviour
{
    public int xSize;
    public int ySize;

    private Vector3[] vertices;
    private Mesh mesh;

    public void Generate(int clothSize = 10, float tileSizeMult = 1f)
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        this.xSize = clothSize;
        this.ySize = clothSize;

        // Generate vertices
        vertices = new Vector3[(xSize + 1) * (ySize + 1)];

        float dispX = (xSize + 1) / 2f;
        float dispY = (ySize + 1) / 2f;

        for (int i = 0, y = 0; y <= ySize; y++)
        {
            for (int x = 0; x <= xSize; x++, i++)
            {
                vertices[i] = new Vector3(x - dispX, y - dispY) * tileSizeMult;
            }
        }
        mesh.vertices = this.vertices;

        // Generate triangles
        int[] triangles = new int[xSize * ySize * 6];
        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++)
        {
            for (int x = 0; x < xSize; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                triangles[ti + 5] = vi + xSize + 2;
            }
        }
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }


    //private void Awake()
    //{
    //    this.Generate();
    //}


    // =========
    // Visualize
    // =========
    //private void OnDrawGizmos()
    //{
    //    if (vertices == null)
    //    {
    //        return;
    //    }

    //    Gizmos.color = Color.black;
    //    for (int i = 0; i < vertices.Length; i++)
    //    {
    //        Gizmos.DrawSphere(vertices[i], 0.1f);
    //    }
    //}
}
