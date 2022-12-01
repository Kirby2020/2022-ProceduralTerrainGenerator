using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class Chunk : MonoBehaviour, IComparer<Chunk> {
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private const int SEA_LEVEL = 40;   // Base terrain height

    public Dictionary<Vector3Int, Block> blocks { get; private set; } = new Dictionary<Vector3Int, Block>(); // Dictionary of blocks in chunk
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

    public void AddBlock(int x, int y, int z) {
        Block block = new GameObject($"Block ({x},\t{y},\t{z})\t").AddComponent<Block>();
        block.SetPosition(x, y, z);
        block.SetParent(transform);
        block.Render();

        blocks.Add(block.position, block);
    }

    // TODO: generate chunk in background thread
    // first, calculate the height of each block in separate thread
    // then, generate the chunk in the main thread
    public Task Generate(FractalNoise terrainNoise) {
        int x = position.x * CHUNK_SIZE; // Get x coordinate of chunk
        int z = position.y * CHUNK_SIZE; // Get z coordinate of chunk
        for (int i = x; i < x + CHUNK_SIZE; i++) {
            for (int j = z; j < z + CHUNK_SIZE; j++) {
                int y = SEA_LEVEL + Mathf.FloorToInt((float)terrainNoise.NoiseCombinedOctaves(i,j) * (float)terrainNoise.Amplitude);                
                AddBlock(i, y, j);
            }
        }
        return Task.CompletedTask;
    }

    public void Fill(int maxHeight = 20) {
        int x = position.x * CHUNK_SIZE; // Get x coordinate of chunk
        int z = position.y * CHUNK_SIZE; // Get z coordinate of chunk
        for (int i = x; i < x + CHUNK_SIZE; i++) {
            for (int j = z; j < z + CHUNK_SIZE; j++) {
                for (int k = 0; k < maxHeight; k++) {
                    AddBlock(i, k, j);
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

    public int GetBlockCount() {
        return blocks.Count;
    }

    int IComparer<Chunk>.Compare(Chunk x, Chunk y){
        // compare each chunk's position
        if (x.position.x == y.position.x && x.position.y == y.position.y) {
            return 0;
        }
        return 1;
    }
}