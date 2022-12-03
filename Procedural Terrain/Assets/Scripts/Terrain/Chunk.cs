using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour, IComparer<Chunk> {
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private const int SEA_LEVEL = 40;   // Base terrain height
    private MeshData meshData = new MeshData();


    private Dictionary<Vector3Int, Block> blocks { get; set; } = new Dictionary<Vector3Int, Block>(); // Dictionary of blocks in chunk
    private int[,] heightMap;           // Height map for chunk
    private Vector2Int position;        // Position of chunk

    public void SetPosition(int x, int z) {
        position = new Vector2Int(x, z);
    }

    public Vector2Int GetPosition() {
        return position;
    }

    public void SetParent(Transform parent) {
        transform.parent = parent;
    }

    private Block CreateBlock(int x, int y, int z) {
        Block block = ScriptableObject.CreateInstance<Block>();
        block.SetPosition(x, y, z);
        block.SetParent(transform);

        blocks.Add(block.Position, block);

        return block;
    }

    public void GenerateHeightMap(FractalNoise terrainNoise) {
        heightMap = new int[CHUNK_SIZE, CHUNK_SIZE];

        int x = position.x * CHUNK_SIZE;
        int z = position.y * CHUNK_SIZE;

        Parallel.For(x, x + CHUNK_SIZE, i => {
            Parallel.For(z, z + CHUNK_SIZE, j => {
                heightMap[i - x, j - z] = 
                    SEA_LEVEL + Mathf.FloorToInt((float)terrainNoise.NoiseCombinedOctaves(i,j) *
                    (float)terrainNoise.Amplitude);
            });
        });
    }

    public void Generate() {
        int x = position.x * CHUNK_SIZE;
        int z = position.y * CHUNK_SIZE;

        // for each element in height map
        for (int i = 0; i < CHUNK_SIZE; i++) {
            for (int j = 0; j < CHUNK_SIZE; j++) {
                int height = heightMap[i, j];
                Block block = CreateBlock(x + i, height, z + j);
            }
        }
    }

    public void GenerateMesh(){
        Vector3 blockPos;
        Block block;

        meshData.ClearData();

        int counter = 0;
        Vector3[] faceVertices = new Vector3[4];
        Vector2[] faceUVs = new Vector2[4];

        foreach (KeyValuePair<Vector3Int, Block> kvp in blocks) {
            if (!kvp.Value.IsSolid) continue;

            blockPos = kvp.Key;
            block = kvp.Value;

            //Iterate over each face direction
            for (int i = 0; i < 6; i++) {     
                //Draw this face

                //Collect the appropriate vertices from the default vertices and add the block position
                for (int j = 0; j < 4; j++) {
                    faceVertices[j] = voxelVertices[voxelVertexIndex[i, j]] + blockPos;
                    faceUVs[j] = voxelUVs[j];
                }

                for (int j = 0; j < 6; j++) {
                    meshData.vertices.Add(faceVertices[voxelTris[i, j]]);
                    meshData.UVs.Add(faceUVs[voxelTris[i, j]]);

                    meshData.triangles.Add(counter++);
                }
            }
        }
    }

    private void UploadMesh() {
        meshData.UploadMesh();

        var meshFilter = GetComponent<MeshFilter>();
        var meshCollider = GetComponent<MeshCollider>();

        meshFilter.mesh = meshData.mesh;

        if (meshData.vertices.Count > 3)
            meshCollider.sharedMesh = meshData.mesh;
    }

    public void Render() {
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
        GenerateMesh();
        UploadMesh();
    }

    public void Fill(int maxHeight = 20) {
        int x = position.x * CHUNK_SIZE; // Get x coordinate of chunk
        int z = position.y * CHUNK_SIZE; // Get z coordinate of chunk
        for (int i = x; i < x + CHUNK_SIZE; i++) {
            for (int j = z; j < z + CHUNK_SIZE; j++) {
                for (int k = 0; k < maxHeight; k++) {
                    Block block = CreateBlock(i, k, j);
                    block.Render();
                }                           
            }
        }
    }

    public void Clear() {
        foreach (KeyValuePair<Vector3Int, Block> block in blocks) {
            block.Value.Destroy();
        }
        blocks.Clear();     
        Destroy(gameObject);    // Remove chunk game object from scene
    }

    #region Mesh Data

    public struct MeshData {
        public Mesh mesh;
        public List<Vector3> vertices;
        public List<int> triangles;
        public List<Vector2> UVs;
        public bool Initialized;

        public void ClearData() {
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
    
    #endregion

    #region Voxel Statics

        static readonly Vector3[] voxelVertices = new Vector3[8] {
            new Vector3(0,0,0),//0
            new Vector3(1,0,0),//1
            new Vector3(0,1,0),//2
            new Vector3(1,1,0),//3

            new Vector3(0,0,1),//4
            new Vector3(1,0,1),//5
            new Vector3(0,1,1),//6
            new Vector3(1,1,1),//7
        };
        static readonly Vector3[] voxelFaceChecks = new Vector3[6] {
            new Vector3(0,0,-1),//back
            new Vector3(0,0,1),//front
            new Vector3(-1,0,0),//left
            new Vector3(1,0,0),//right
            new Vector3(0,-1,0),//bottom
            new Vector3(0,1,0)//top
        };

        static readonly int[,] voxelVertexIndex = new int[6, 4] {
            {0,1,2,3},
            {4,5,6,7},
            {4,0,6,2},
            {5,1,7,3},
            {0,1,4,5},
            {2,3,6,7},
        };

        static readonly Vector2[] voxelUVs = new Vector2[4] {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,0),
            new Vector2(1,1)
        };

        static readonly int[,] voxelTris = new int[6, 6] {
            {0,2,3,0,3,1},
            {0,1,2,1,3,2},
            {0,2,3,0,3,1},
            {0,1,2,1,3,2},
            {0,1,2,1,3,2},
            {0,2,3,0,3,1},
        };

        #endregion
    
    public int GetBlockCount() {
        return blocks.Count;
    }

    public int GetVertexCount() {
        return GetComponent<MeshFilter>().mesh.vertexCount;
    }

    int IComparer<Chunk>.Compare(Chunk x, Chunk y){
        // compare each chunk's position
        if (x.position.x == y.position.x && x.position.y == y.position.y) {
            return 0;
        }
        return 1;
    }
}