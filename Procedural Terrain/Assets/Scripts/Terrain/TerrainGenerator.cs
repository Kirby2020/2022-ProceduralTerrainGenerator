using System;
using System.Threading;
using System.Collections;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityEngine.Profiling;
using System.Collections.Concurrent;

public class TerrainGenerator : MonoBehaviour {
    [SerializeField] private Transform player;
    private Queue<Chunk> chunksToGenerate = new Queue<Chunk>(); // Queue of chunks to generate
    private Queue<Chunk> chunksToRender = new Queue<Chunk>();   // Queue of chunks to render
    private Queue<Chunk> chunksOverflowBuffer = new Queue<Chunk>();   // Overflow buffer for chunks
    private ConcurrentQueue<Chunk> renderedChunks = new ConcurrentQueue<Chunk>();  // Queue of chunks that have been generated
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
        GenerateSpawnChunks();   // Generates spawn chunks

        InvokeRepeating("UpdateInspector", 0, 5);  // Updates inspector every second
    }

    private void Update() {
        // Generate extra chunks as player moves
        GeneratePlayerChunks();

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
                chunksToRender.Enqueue(chunk);
            }
        }

        StartCoroutine(GenerateChunks());
        StartCoroutine(RenderChunks());
    }

    private IEnumerator GenerateChunks() {
        int maxChunksToGenerate = RENDER_DISTANCE * RENDER_DISTANCE;

        if (chunksToGenerate.Count > maxChunksToGenerate) {
            // If there are too many chunks to generate,
            // Add chunks to overflow queue and generate them later
            Debug.LogWarning("Too many chunks to generate, adding to overflow buffer: " + chunksOverflowBuffer.Count);
            for (int i = 0; i < chunksToGenerate.Count - maxChunksToGenerate; i++) {
                Chunk chunk = chunksToGenerate.Dequeue();
                chunksOverflowBuffer.Enqueue(chunk);
            }
            yield return null;
        }
        while (chunksToGenerate.Count > 0) {
            Debug.Log("Remaining chunks to generate: " + chunksToGenerate.Count);
            Chunk chunk = chunksToGenerate.Dequeue();
            chunk.GenerateHeightMap(terrainNoise);
            chunk.Generate();
            yield break;
        }
        if (chunksToGenerate.Count == 0 && chunksOverflowBuffer.Count > 0) {
            Debug.Log("Emptying overflow buffer");
            while (chunksOverflowBuffer.Count > 0) {
                chunksToGenerate.Enqueue(chunksOverflowBuffer.Dequeue());
                yield break;
            }
        }
        
    }

    private IEnumerator RenderChunks() {
        while (chunksToRender.Count > 0) {
            Chunk chunk = chunksToRender.Dequeue();
            RenderChunk(chunk);
            yield break;
        }
    }

    /// <summary>
    /// Create chunk game object and attach Chunk script.
    /// </summary> 
    /// <returns>Chunk container for the given coordinate or null if one already exists</returns>
    /// <param name="chunkX">X position of the chunk</param>
    /// <param name="chunkZ">Z position of the chunk</param>
    private Chunk CreateChunk(int chunkX, int chunkZ) {
        if (ChunkExists(chunkX, chunkZ)) return null;  // Chunk already exists

        // Else: create a new chunk
        Chunk chunk = new GameObject($"Chunk {chunkX}, {chunkZ}").AddComponent<Chunk>();
        chunk.SetPosition(chunkX, chunkZ);
        chunk.SetParent(transform);
        chunk.Initialize();

        return chunk;
    }

    private bool ChunkExists(int chunkX, int chunkZ) {
        // If a chunk already exists at this position, return null
        if (renderedChunks.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return true;
        if (chunksToGenerate.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return true;
        if (chunksToRender.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return true;
        return false;
    }

    /// <summary>
    /// Render chunk ans add it to the queue of generated chunks.
    /// </summary>
    /// <param name="chunk">Chunk to render</param>
    private void RenderChunk(Chunk chunk) {
        if (chunk == null) return; // Chunk not generated yet
        // Render chunk
        chunk.Render();

        // Add chunk to queue of generated chunks
        renderedChunks.Enqueue(chunk);
    }

    /// <summary>
    /// Remove chunks that are too far away from the player.
    /// </summary>
    private void UnloadChunks() {
        while (renderedChunks.Count > 20 * RENDER_DISTANCE * RENDER_DISTANCE) {
            renderedChunks.TryDequeue(out Chunk chunk);
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

    private void UpdateInspector() {
        chunksInspector = renderedChunks.Select(chunk => new Vector2(chunk.GetPosition().x, chunk.GetPosition().y)).ToList();
        blockCount = renderedChunks.Sum(chunk => chunk.GetBlockCount());
        TotalVertices = renderedChunks.Sum(chunk => chunk.GetVertexCount());
    }


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
        if (renderedChunks.Any(chunk => chunk.GetPosition().x == chunkX && chunk.GetPosition().y == chunkZ)) {
            return;
        }

        // Create chunk game object and attach Chunk script
        Chunk chunk = new GameObject("Chunk " + chunkX + ", " + chunkZ).AddComponent<Chunk>();

        // Fill chunk
        chunk.SetPosition(chunkX, chunkZ);
        chunk.SetParent(transform);
        chunk.Fill();

        // Add chunk to queue of generated chunks
        renderedChunks.Enqueue(chunk);
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
