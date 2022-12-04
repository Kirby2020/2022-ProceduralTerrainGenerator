using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Events;

public class TerrainGenerator : MonoBehaviour {
    [SerializeField] private Transform player;
    private Queue<Chunk> generatedChunks = new Queue<Chunk>();  // Queue of chunks that have been generated
    [SerializeField] private List<Vector2> chunksInspector = new List<Vector2>();  // List of chunks that have been generated
    [SerializeField] private int blockCount = 0;
    [SerializeField] private int TotalVertices = 0;
    private FractalNoise terrainNoise;  // Main noise map for terrain height
    private const int MAX_HEIGHT = 40;  // Maximum height of terrain
    private const int MIN_HEIGHT = 0;   // Minimum height of terrain
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private const int RENDER_DISTANCE = 16;     // How many chunks to render around player
    private const int SEA_LEVEL = 40;          // Base terrain height

    private Thread chunkGeneratorThread;  // Thread for generating chunks


    private void Awake() {
        SetTerrainNoise();
        generatedChunks = new Queue<Chunk>();
        
        GenerateSpawnChunks();   // Generates spawn chunks
    }

    private void Update() {
        // Generate extra chunks as player moves
        GeneratePlayerChunks();

        // Remove chunks that are too far away
        UnloadChunks();
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

    #region Chunk Generation
    private void GenerateSpawnChunks() {
        int min = -RENDER_DISTANCE / 2;
        int max = RENDER_DISTANCE / 2;

        for (int chunkX = min; chunkX < max; chunkX++) {
            for (int chunkZ = min; chunkZ < max; chunkZ++) {
                Chunk chunk = CreateChunk(chunkX, chunkZ);
                chunk.GenerateHeightMap(terrainNoise);
                chunk.Generate();
                RenderChunk(chunk);
            }
        }
    }

    private void GeneratePlayerChunks() {
        var chunksToGenerate = new Queue<Chunk>();

        // Get chunk coordinates of player
        Vector2Int playerChunk = GetChunkPosition(player.position);

        int min = -RENDER_DISTANCE / 2;  
        int max = RENDER_DISTANCE / 2;

        // Load all chunks around player with radius min to max ( = renderDistance )
        for (int chunkX = min; chunkX < max; chunkX++) {
            for (int chunkZ = min; chunkZ < max; chunkZ++) {
                Vector2Int chunkPosition = new Vector2Int(playerChunk.x + chunkX, playerChunk.y + chunkZ);
                Chunk chunk = CreateChunk(chunkPosition.x, chunkPosition.y);
                if (chunk == null) continue;    // Chunk already exists, no need to generate
                chunksToGenerate.Enqueue(chunk);
            }
        }

        // Generate chunks in parallel
        Parallel.ForEach(chunksToGenerate, chunk => {
            chunk.GenerateHeightMap(terrainNoise);
            chunk.Generate();
        });

        // Render chunks
        foreach (Chunk chunk in chunksToGenerate) {
            RenderChunk(chunk);
        }
    }

    /// <summary>
    /// Create chunk game object and attach Chunk script.
    /// </summary> 
    /// <returns>Chunk container for the given coordinate or null if one already exists</returns>
    /// <param name="chunkX">X position of the chunk</param>
    /// <param name="chunkZ">Z position of the chunk</param>
    private Chunk CreateChunk(int chunkX, int chunkZ) {
        // If a chunk already exists at this position, return null
        if (generatedChunks.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return null;

        // Else: create a new chunk
        Chunk chunk = new GameObject($"Chunk {chunkX}, {chunkZ}").AddComponent<Chunk>();
        chunk.SetPosition(chunkX, chunkZ);
        chunk.SetParent(transform);
        chunk.Initialize();

        return chunk;
    }

    /// <summary>
    /// Render chunk ans add it to the queue of generated chunks.
    /// </summary>
    /// <param name="chunk">Chunk to render</param>
    private void RenderChunk(Chunk chunk) {
        // Add chunk to queue of generated chunks
        generatedChunks.Enqueue(chunk);

        // Render chunk
        chunk.Render();

        // Update inspector
        chunksInspector.Add(chunk.GetPosition());
        blockCount = generatedChunks.Sum(chunk => chunk.GetBlockCount());
        TotalVertices += chunk.GetVertexCount();
    }

    /// <summary>
    /// Remove chunks that are too far away from the player.
    /// </summary>
    private void UnloadChunks() {
        while (generatedChunks.Count > 20 * RENDER_DISTANCE * RENDER_DISTANCE) {
            Chunk chunk = generatedChunks.Dequeue();
            chunksInspector.Remove(chunk.GetPosition());  // For inspector only
            chunk.Clear();
        }
    }

    #endregion
    
    private Vector3Int GetPlayerPosition() {
        return new Vector3Int(Mathf.FloorToInt(player.position.x), Mathf.FloorToInt(player.position.y), Mathf.FloorToInt(player.position.z));
    }

    private Vector2Int GetChunkPosition(Vector3 position) {
        return new Vector2Int(Mathf.FloorToInt(position.x / CHUNK_SIZE), Mathf.FloorToInt(position.z / CHUNK_SIZE));
    }

    #region Event Handlers
    
    #endregion

    #region demos
    private void GenerateTerrain() {
        int minX = (int)player.position.x;
        int minZ = (int)player.position.z;
        for (int x = minX; x < (int)player.position.x + CHUNK_SIZE + (int)player.position.x; x++) {
            for (int z = minZ; z < (int)player.position.z + CHUNK_SIZE + (int)player.position.z; z++) {
                int y = SEA_LEVEL + Mathf.FloorToInt((float)terrainNoise.NoiseCombinedOctaves(x,z) * (float)terrainNoise.Amplitude);
                // if (y < MIN_HEIGHT || y > MAX_HEIGHT) break;
                PlaceBlock(x, y, z);
                //FillUnderground(x, y, z);
            }
        }
    }

    private void GenerateFlatTerrain() {
        for (int x = 0; x < CHUNK_SIZE; x++) {
            for (int z = 0; z < CHUNK_SIZE; z++) {
                for (int y = 0; y < SEA_LEVEL; y++) {
                    PlaceBlock(x, y, z);
                }
            }
        }
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
        chunk.SetParent(transform);
        chunk.Fill();

        // Add chunk to queue of generated chunks
        generatedChunks.Enqueue(chunk);
        chunksInspector.Add(chunk.GetPosition());  // For inspector only
    }

    private void PlaceBlock(int x, int y, int z) {
        // Block block = ScriptableObject.CreateInstance<Block>();
        // block.SetPosition(x, y, z);
        // block.SetParent(transform);
        // block.Render();
    }

    private void FillUnderground(int x, int y, int z) {
        for (int i = MIN_HEIGHT; i < y; i++) {
            PlaceBlock(x, i, z);
        }
    }
    #endregion
}
