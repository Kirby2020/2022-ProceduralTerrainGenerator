using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
[RequireComponent(typeof(Chunk))]
public class Chunk : MonoBehaviour, IComparer<Chunk> {
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private const int MAX_HEIGHT = 100;  // Maximum height of terrain
    private const int SEA_LEVEL = 60;   // Base terrain height
    private const int MIN_HEIGHT = 0;   // Minimum height of terrain
    private MeshData meshData = new MeshData(); // Mesh data for chunk
    private MeshRenderer meshRenderer;  
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private Material material;
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

    public void SetMaterial(Material material) {
        this.material = material;
    }

    public int GetBlockCount() {
        return blocks.Count;
    }

    public int GetVertexCount() {
        return GetComponent<MeshFilter>().mesh.vertexCount;
    }
    #endregion

    public void Initialize() {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

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
                for (int k = MIN_HEIGHT; k <= height; k++) {
                    // Top block
                    if (k == height) {
                        CreateBlock(i, k, j, BlockType.Grass);
                    }
                    else if (k > height - 3) {
                        CreateBlock(i, k, j, BlockType.Dirt);
                    }
                    else if (k == MIN_HEIGHT) {
                        CreateBlock(i, k, j, BlockType.Bedrock);
                    }
                    else {
                        CreateBlock(i, k, j, BlockType.Stone);
                    }
                }
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

    /// <summary>
    /// Removes all blocks from chunk
    /// </summary>
    public void Clear() {
        blocks.Clear();     
        Destroy(gameObject);    // Remove chunk game object from scene
    }
    
    public void Render() {
        meshRenderer.sharedMaterial = material ?? new Material(Shader.Find("Standard"));
        Profiler.BeginSample("Generating mesh");
        GenerateOptimizedMesh();
        Profiler.EndSample();
        Profiler.BeginSample("Applying mesh");
        UploadMesh();
        Profiler.EndSample();
    }

    /// <summary>
    /// Generates a mesh for the chunk
    /// </summary>
    private void GenerateMesh(){
        Vector3 blockPos;
        Block block;
        Color color;
        Vector2 smoothness = new Vector2(0, 0);

        meshData.ClearData();

        int counter = 0;
        Vector3[] faceVertices = new Vector3[4];
        Vector2[] faceUVs = new Vector2[4];

        foreach (KeyValuePair<Vector3Int, Block> kvp in blocks) {
            blockPos = kvp.Key;
            block = kvp.Value;     
            color = block.Color;  

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
                    meshData.UVs2.Add(smoothness);
                    meshData.triangles.Add(counter++);
                    meshData.colors.Add(color);
                }
            }
        }
    }

    /// <summary>
    /// Generates an optimized mesh by looking at neighboring blocks and only drawing faces that are visible.
    /// </summary>
    private void GenerateOptimizedMesh(){
        Vector3Int blockPos;
        Block block;
        Color color;
        Vector2 smoothness = new Vector2(0, 0);

        meshData.ClearData();

        int counter = 0;
        Vector3[] faceVertices = new Vector3[4];
        Vector2[] faceUVs = new Vector2[4];
        Block[] neighbors = new Block[6];

        foreach (KeyValuePair<Vector3Int, Block> kvp in blocks) {
            Profiler.BeginSample("Iterating over block");
            blockPos = kvp.Key;
            block = kvp.Value;    
            color = block.Color;        

            // Get neighboring blocks
            neighbors = GetNeighbors(blockPos);

            // Iterate over each face direction
            for (int directionIndex = 0; directionIndex < 6; directionIndex++) {   
                // Collect the appropriate vertices from the default vertices and add the block position
                for (int vertexIndex = 0; vertexIndex < 4; vertexIndex++) {
                    faceVertices[vertexIndex] = VoxelData.voxelVertices[VoxelData.voxelVertexIndex[directionIndex, vertexIndex]] + blockPos;
                    faceUVs[vertexIndex] = VoxelData.voxelUVs[vertexIndex];
                }

                // If neighbor is empty or not solid
                if (neighbors[directionIndex] == null || !neighbors[directionIndex].IsSolid) {
                    // Draw this face
                    for (int vertexIndex = 0; vertexIndex < 6; vertexIndex++) {
                        meshData.vertices.Add(faceVertices[VoxelData.voxelTris[directionIndex, vertexIndex]]);
                        meshData.colors.Add(color);
                        meshData.UVs.Add(faceUVs[VoxelData.voxelTris[directionIndex, vertexIndex]]);
                        meshData.UVs2.Add(smoothness);
                        meshData.triangles.Add(counter++);
                    }
                }
            }
            Profiler.EndSample();
        }
    }

    private void UploadMesh() {
        meshData.UploadMesh();

        meshFilter.mesh = meshData.mesh;

        if (meshData.vertices.Count > 3) {}
            //meshCollider.sharedMesh = meshData.mesh;
    }

    private Block[] GetNeighbors(Vector3Int blockPos) {
        Block[] neighbors = new Block[6];

        for (int i = 0; i < 6; i++) {
            // Vector3Int neighborPos = blockPos + VoxelData.faceChecks[i];
            // TODO: cast all VoxelData to Vector3Int
            Vector3Int neighborPos = blockPos + new Vector3Int(
                Mathf.FloorToInt(VoxelData.voxelFaceChecks[i].x), 
                Mathf.FloorToInt(VoxelData.voxelFaceChecks[i].y),
                Mathf.FloorToInt(VoxelData.voxelFaceChecks[i].z)
            );
            neighbors[i] = GetBlock(neighborPos);
        }

        return neighbors;
    }

    private Block GetBlock(Vector3Int blockPos) {
        if (blocks.TryGetValue(blockPos, out Block block)) {
            return block;
        }

        return null;
    }

    private Vector3Int GetChunkCoordinates() {
        return new Vector3Int(position.x * CHUNK_SIZE, 0, position.y * CHUNK_SIZE);
    }

    private Block CreateBlock(int x, int y, int z, BlockType type = BlockType.Stone) {
        Block block = CreateBlockFromType(type);
        block.SetPosition(x, y, z);

        blocks.TryAdd(block.Position, block);

        return block;
    }

    private Block CreateBlockFromType(BlockType type) {
        switch (type) {
            case BlockType.Stone: return new StoneBlock();
            case BlockType.Grass: return new GrassBlock();
            case BlockType.Dirt : return new DirtBlock();
            default: return new StoneBlock();
        }
    }

    int IComparer<Chunk>.Compare(Chunk x, Chunk y){
        // compare each chunk's position
        if (x.position.x == y.position.x && x.position.y == y.position.y) {
            return 0;
        }
        return 1;
    }
}
