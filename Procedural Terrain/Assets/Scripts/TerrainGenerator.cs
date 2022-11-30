using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

public class TerrainGenerator : MonoBehaviour {
    [SerializeField] private Transform player;
    private Queue<Chunk> generatedChunks = new Queue<Chunk>();  // Queue of chunks that have been generated
    [SerializeField] private List<Vector2> chunksInspector = new List<Vector2>();  // List of chunks that have been generated
    [SerializeField] private int blockCount = 0;
    private FractalNoise terrainNoise;  // Main noise map for terrain height
    private const int MAX_HEIGHT = 40;  // Maximum height of terrain
    private const int MIN_HEIGHT = 0;   // Minimum height of terrain
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private int renderDistance = 8;     // How many chunks to render around player
    private int seaLevel = 40;          // Base terrain height

    private void Awake() {
        SetTerrainNoise();
        generatedChunks = new Queue<Chunk>();
        // GenerateTerrain();
        GenerateSpawnChunks();   // Generates spawn chunks
    }

    private async void Update() {
        // Generate extra chunks as player moves
        // create a background task
        await Task.Run(() => {
            GeneratePlayerChunks();
        });
        // Remove chunks that are too far away
        // UnloadChunks();
    }

    private void SetTerrainNoise() {
        terrainNoise = ScriptableObject.CreateInstance<FractalNoise>();
        terrainNoise.SetSeed(0);
        terrainNoise.Amplitude = MAX_HEIGHT;
        terrainNoise.Frequency = 0.005f;
        terrainNoise.Octaves = 4;
        terrainNoise.Lacunarity = 2f;
        terrainNoise.Persistence = 0.5f;        
    }

    private void GenerateSpawnChunks() {
        int min = -renderDistance / 2;
        int max = renderDistance / 2;

        for (int chunkX = min; chunkX < max; chunkX++) {
            for (int chunkZ = min; chunkZ < max; chunkZ++) {
                GenerateChunk(chunkX, chunkZ);
            }
        }
    }

    private void GeneratePlayerChunks() {
        // Get chunk coordinates of player
        Vector2Int playerChunk = GetChunkPosition(player.position);

        int min = -renderDistance / 2;  
        int max = renderDistance / 2;

        // Generate chunks around player with radius min to max ( = renderDistance )
        for (int chunkX = min; chunkX <= max; chunkX++) {
            for (int chunkZ = min; chunkZ <= max; chunkZ++) {
                GenerateChunk(playerChunk.x + chunkX, playerChunk.y + chunkZ);
            }
        }
    }

    /// <summary>
    /// Generates a chunk at the given position
    /// </summary>
    /// <param name="chunkX">X position of the chunk</param>
    /// <param name="chunkZ">Z position of the chunk</param>
    private void GenerateChunk(int chunkX, int chunkZ) {
        // Check if chunk has already been generated
        if (generatedChunks.Any(chunk => chunk.GetPosition().x == chunkX && chunk.GetPosition().y == chunkZ)) {
            return;
        }

        // Create chunk game object and attach Chunk script
        Chunk chunk = new GameObject("Chunk " + chunkX + ", " + chunkZ).AddComponent<Chunk>();

        // Generate chunk
        chunk.SetPosition(chunkX, chunkZ);
        chunk.Generate(terrainNoise);

        // Add chunk to queue of generated chunks
        generatedChunks.Enqueue(chunk);
        chunksInspector.Add(chunk.GetPosition());  // For inspector only
        blockCount = generatedChunks.Sum(chunk => chunk.GetBlockCount());
    }

    /// <summary>
    /// Fills a chunk with blocks
    /// Only used for demonstration purposes
    /// </summary>
    /// <param name="chunkX">X position of the chunk</param>
    /// <param name="chunkZ">Z position of the chunk</param>
    private void FillChunk(int chunkX, int chunkZ) {
        // Check if chunk has already been generated
        if (generatedChunks.Any(chunk => chunk.GetPosition().x == chunkX && chunk.GetPosition().y == chunkZ)) {
            return;
        }

        // Create chunk game object and attach Chunk script
        Chunk chunk = new GameObject("Chunk " + chunkX + ", " + chunkZ).AddComponent<Chunk>();

        // Fill chunk
        chunk.SetPosition(chunkX, chunkZ);
        chunk.Fill(20);

        // Add chunk to queue of generated chunks
        generatedChunks.Enqueue(chunk);
        chunksInspector.Add(chunk.GetPosition());  // For inspector only
    }

    private void UnloadChunks() {
        while (generatedChunks.Count > renderDistance * renderDistance) {
            Chunk chunk = generatedChunks.Dequeue();
            chunk.Clear();
        }
    }
    
    private Vector3Int GetPlayerPosition() {
        return new Vector3Int(Mathf.FloorToInt(player.position.x), Mathf.FloorToInt(player.position.y), Mathf.FloorToInt(player.position.z));
    }

    private Vector2Int GetChunkPosition(Vector3 position) {
        return new Vector2Int(Mathf.FloorToInt(position.x / CHUNK_SIZE), Mathf.FloorToInt(position.z / CHUNK_SIZE));
    }

    #region old
    private void GenerateTerrain() {
        int minX = (int)player.position.x;
        int minZ = (int)player.position.z;
        for (int x = minX; x < (int)player.position.x + CHUNK_SIZE + (int)player.position.x; x++) {
            for (int z = minZ; z < (int)player.position.z + CHUNK_SIZE + (int)player.position.z; z++) {
                int y = seaLevel + Mathf.FloorToInt((float)terrainNoise.NoiseCombinedOctaves(x,z) * (float)terrainNoise.Amplitude);
                // if (y < MIN_HEIGHT || y > MAX_HEIGHT) break;
                PlaceBlock(x, y, z);
                //FillUnderground(x, y, z);
            }
        }
    }

    private void PlaceBlock(int x, int y, int z) {
        Block block = ScriptableObject.CreateInstance<Block>();
        block.SetPosition(x, y, z);
        block.SetParent(transform);
        block.Place();
    }

    private void FillUnderground(int x, int y, int z) {
        for (int i = MIN_HEIGHT; i < y; i++) {
            PlaceBlock(x, i, z);
        }
    }
    #endregion
}
