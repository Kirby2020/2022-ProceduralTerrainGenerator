using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : ScriptableObject {
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private const int SEA_LEVEL = 40;          // Base terrain height


    public Dictionary<Vector3Int, Block> blocks { get; private set; } = new Dictionary<Vector3Int, Block>(); // Dictionary of blocks in chunk
    public Vector2Int position { get; private set; } // Position of chunk
    public Transform chunkContainer { get; set; } // Parent of chunk

    public void SetPosition(int x, int z) {
        position = new Vector2Int(x, z);
        SetName();
    }

    public void SetParent(Transform parent) {
        chunkContainer = parent;
    }

    private void SetName() {
        name = $"Chunk ({position.x},\t{position.y})\t";
    }

    public void AddBlock(int x, int y, int z) {
        Block block = ScriptableObject.CreateInstance<Block>();
        block.SetPosition(x, y, z);
        block.SetParent(chunkContainer);
        block.Place();

        blocks.Add(block.position, block);
        
    }

    public void Generate(FractalNoise terrainNoise) {
        int x = position.x * CHUNK_SIZE; // Get x coordinate of chunk
        int z = position.y * CHUNK_SIZE; // Get z coordinate of chunk
        for (int i = x; i < x + CHUNK_SIZE; i++) {
            for (int j = z; j < z + CHUNK_SIZE; j++) {
                int y = SEA_LEVEL + Mathf.FloorToInt((float)terrainNoise.NoiseCombinedOctaves(i,j) * (float)terrainNoise.Amplitude);                
                AddBlock(i, y, j);
            }
        }
    }

    public void ClearChunk() {
        foreach (KeyValuePair<Vector3Int, Block> block in blocks) {
            block.Value.Destroy();
        }
    }

}