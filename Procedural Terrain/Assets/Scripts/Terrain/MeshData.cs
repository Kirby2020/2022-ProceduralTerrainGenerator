using System.Collections.Generic;
using UnityEngine;

public struct MeshData {
    public Mesh mesh;
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector2> UVs;
    public bool Initialized;

    public void ClearData(){
        if (!Initialized) {
            vertices = new List<Vector3>();
            triangles = new List<int>();
            UVs = new List<Vector2>();

            Initialized = true;
            mesh = new Mesh();
        }
        else {
            vertices.Clear();
            triangles.Clear();
            UVs.Clear();

            mesh.Clear();
        }
    }

    public void UploadMesh(bool sharedVertices = false) {
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0, false);

        mesh.SetUVs(0, UVs);

        mesh.Optimize();

        mesh.RecalculateNormals();

        mesh.RecalculateBounds();

        mesh.UploadMeshData(false);
    }
}
