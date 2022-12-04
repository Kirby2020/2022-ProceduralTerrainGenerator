using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
[RequireComponent(typeof(Chunk))]
public class Chunk : MonoBehaviour, IComparer<Chunk> {
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private const int MAX_HEIGHT = 60;  // Maximum height of terrain
    private const int SEA_LEVEL = 30;   // Base terrain height
    private const int MIN_HEIGHT = 0;   // Minimum height of terrain
    private MeshData meshData = new MeshData(); // Mesh data for chunk
    private ConcurrentDictionary<Vector3Int, Block> blocks { get; set; } = new ConcurrentDictionary<Vector3Int, Block>(); // Dictionary of blocks in chunk
    private int[,] heightMap;           // Height map for chunk
    private Vector2Int position;        // Position of chunk

    #region Getters & Setters
    public void SetPosition(int x, int z) {
        position = new Vector2Int(x, z);
    }

    public Vector2Int GetPosition() {
        return position;
    }

    public void SetParent(Transform parent) {
        transform.parent = parent;
    }

    public int GetBlockCount() {
        return blocks.Count;
    }

    public int GetVertexCount() {
        return GetComponent<MeshFilter>().mesh.vertexCount;
    }
    #endregion

    public void GenerateHeightMap(FractalNoise terrainNoise) {
        heightMap = new int[CHUNK_SIZE, CHUNK_SIZE];
        var chunkCoordinates = GetChunkCoordinates();

        Parallel.For(chunkCoordinates.x, chunkCoordinates.x + CHUNK_SIZE, i => {
            Parallel.For(chunkCoordinates.z, chunkCoordinates.z + CHUNK_SIZE, j => {
                // Subtract chunk coordinates to get local coordinates in chunk (0,0) to (ChunkSize - 1, ChunkSize - 1)
                heightMap[i - chunkCoordinates.x, j - chunkCoordinates.z] = 
                    SEA_LEVEL + Mathf.FloorToInt((float)terrainNoise.NoiseCombinedOctaves(i,j) *
                    (float)terrainNoise.Amplitude);
            });
        });
    }

    public void Generate() {
        var chunkCoordinates = GetChunkCoordinates();
        
        Parallel.For(chunkCoordinates.x, chunkCoordinates.x + CHUNK_SIZE, i => {
            Parallel.For(chunkCoordinates.z, chunkCoordinates.z + CHUNK_SIZE, j => {
                int height = heightMap[i - chunkCoordinates.x, j - chunkCoordinates.z];
                Block block = CreateBlock(i, height, j);
            });
        });
    }
    
    public void Fill() {
        var chunkCoordinates = GetChunkCoordinates();

        for (int x = chunkCoordinates.x; x < chunkCoordinates.x + CHUNK_SIZE; x++) {
            for (int z = chunkCoordinates.z; z < chunkCoordinates.z + CHUNK_SIZE; z++) {
                for (int y = MIN_HEIGHT; y < MAX_HEIGHT; y++) {
                    Block block = CreateBlock(x, y, z);
                }                           
            }
        }
    }

    public void Clear() {
        blocks.Clear();     
        Destroy(gameObject);    // Remove chunk game object from scene
    }
    
    public void Render() {
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Standard"));
        GenerateMesh();
        UploadMesh();
    }

    private void GenerateMesh(){
        Vector3 blockPos;
        Block block;

        meshData.ClearData();

        int counter = 0;
        Vector3[] faceVertices = new Vector3[4];
        Vector2[] faceUVs = new Vector2[4];

        foreach (KeyValuePair<Vector3Int, Block> kvp in blocks) {
            blockPos = kvp.Key;
            block = kvp.Value;

            if (!block.IsSolid) continue;            

            //Iterate over each face direction
            for (int i = 0; i < 6; i++) {     
                //Draw this face

                //Collect the appropriate vertices from the default vertices and add the block position
                for (int j = 0; j < 4; j++) {
                    faceVertices[j] = VoxelData.voxelVertices[VoxelData.voxelVertexIndex[i, j]] + blockPos;
                    faceUVs[j] = VoxelData.voxelUVs[j];
                }

                for (int j = 0; j < 6; j++) {
                    meshData.vertices.Add(faceVertices[VoxelData.voxelTris[i, j]]);
                    meshData.UVs.Add(faceUVs[VoxelData.voxelTris[i, j]]);

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

    private Vector3Int GetChunkCoordinates() {
        return new Vector3Int(position.x * CHUNK_SIZE, 0, position.y * CHUNK_SIZE);
    }

    private Block CreateBlock(int x, int y, int z) {
        Block block = new StoneBlock();
        block.SetPosition(x, y, z);

        blocks.TryAdd(block.Position, block);

        return block;
    }

    int IComparer<Chunk>.Compare(Chunk x, Chunk y){
        // compare each chunk's position
        if (x.position.x == y.position.x && x.position.y == y.position.y) {
            return 0;
        }
        return 1;
    }
}
