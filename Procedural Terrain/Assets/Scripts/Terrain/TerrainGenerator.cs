using System;
using System.Threading;
using System.Collections;
using Unity.Collections;
using Unity.Mathematics;
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
    [SerializeField] private Material worldMaterial;
    private ConcurrentQueue<Chunk> chunksToGenerate = new ConcurrentQueue<Chunk>(); // Queue of chunks to generate
    private ConcurrentQueue<Chunk> chunksToRender = new ConcurrentQueue<Chunk>();   // Queue of chunks to render
    private ConcurrentQueue<Chunk> chunksOverflowBuffer = new ConcurrentQueue<Chunk>();   // Overflow buffer for chunks
    private ConcurrentQueue<Chunk> renderedChunks = new ConcurrentQueue<Chunk>();  // Queue of chunks that have been generated
    [SerializeField] private List<Vector2> chunksInspector = new List<Vector2>();  // List of chunks that have been generated
    [SerializeField] private int blockCount = 0;
    [SerializeField] private int TotalVertices = 0;
    private FractalNoise terrainNoise;  // Main noise map for terrain height
    private TerrainNoise terrainNoiseTest; 
    private const int RENDER_DISTANCE = 6;     // How many chunks to render around player
    private Thread chunkCreatorThread;
    private Thread chunkGeneratorThread;
    private Thread chunkRendererThread;

    private void Awake() {
        SetTerrainNoise();
        // GenerateSpawnChunks();   // Generates spawn chunks

        StartCoroutine(GeneratePlayerChunks());
        InvokeRepeating("UpdateInspector", 0, 5);  // Updates inspector every second
    }


    private void Update() {
        // Remove chunks that are too far away
        // UnloadChunks();
    }

    private void SetTerrainNoise() {
        terrainNoiseTest = new TerrainNoise(0);
        
        terrainNoise = new FractalNoise();
        terrainNoise.SetSeed(0);
        terrainNoise.Amplitude = 50f;
        terrainNoise.Frequency = 0.003f;
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
                chunk.GenerateHeightMap(terrainNoiseTest);
                chunk.Generate();
                RenderChunk(chunk);
            }
        }
    }

    private IEnumerator GeneratePlayerChunks() {
        var chunkList = new ConcurrentDictionary<Vector2Int, int>();

        while (true) {
            Debug.Log("Chunklist: " + chunkList.Count);

            Profiler.BeginSample("Creating chunks");

            StartCoroutine(CreateChunks(chunkList));

            Profiler.EndSample();
            // Profiler.BeginSample("Generating chunks");

            // // StartCoroutine(GenerateChunks());
            // GenerateChunksInBackground();

            // Profiler.EndSample();
            // Profiler.BeginSample("Rendering chunks");

            // // StartCoroutine(RenderChunks());

            // Profiler.EndSample();

            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Creates chunks around player
    /// </summary>
    /// <param name="chunkList">List of chunk positions and their distance from player</param>
    private IEnumerator CreateChunks(ConcurrentDictionary<Vector2Int, int> chunkList) {
        int min = -RENDER_DISTANCE / 2;  
        int max = RENDER_DISTANCE / 2;

        Vector2Int playerChunk = GetChunkPosition(player.position);      
        
        Parallel.For(min, max, chunkX => {
            Parallel.For(min, max, chunkZ => {
                Vector2Int position = new Vector2Int(playerChunk.x + chunkX, playerChunk.y + chunkZ);
                if (ChunkExists(position.x, position.y)) return;
                int chunkDistance = math.abs(chunkX) + math.abs(chunkZ);
                chunkList.TryAdd(position, chunkDistance);
            });
        });

        // Sort chunks by distance from player chunk
        chunkList = new ConcurrentDictionary<Vector2Int, int>(chunkList.OrderBy(x => x.Value));

        if (chunkList.Count == 0) yield return null;            

        Vector2Int chunkPosition;
        Chunk chunk;
        int distance;

        // Load all chunks around player with radius min to max ( = renderDistance )
        // Starting from the chunk closest to the player
        foreach (KeyValuePair<Vector2Int, int> kvp in chunkList) {
            chunkPosition = kvp.Key;
            distance = kvp.Value;

            chunk = CreateChunk(chunkPosition.x, chunkPosition.y);
            chunksToGenerate.Enqueue(chunk);

            yield break;
        }
    }    

    private IEnumerator GenerateChunks() {
        int maxChunksToGenerate = RENDER_DISTANCE * RENDER_DISTANCE;

        if (chunksToGenerate.Count > maxChunksToGenerate) {
            // If there are too many chunks to generate,
            // Add chunks to overflow queue and generate them later
            // This gives priority to chunks that are closer to the player
            Debug.LogWarning("Too many chunks to generate, adding to overflow buffer: " + chunksOverflowBuffer.Count);
            while (chunksToGenerate.Count > 0) {
                chunksOverflowBuffer.Enqueue(chunksToGenerate.TryDequeue(out Chunk chunk) ? chunk : null);
                yield break;
            }
        }
        while (chunksToGenerate.Count > 0) {
            Debug.Log("Remaining chunks to generate: " + chunksToGenerate.Count);
            chunksToGenerate.TryDequeue(out Chunk chunk);
            chunk.GenerateHeightMap(terrainNoise);
            chunk.Generate();
            chunksToRender.Enqueue(chunk);
            yield break;
        }
        if (chunksToGenerate.Count == 0 && chunksOverflowBuffer.Count > 0) {
            Debug.Log("Emptying overflow buffer");
            while (chunksOverflowBuffer.Count > 0) {
                chunksOverflowBuffer.TryDequeue(out Chunk chunk);
                chunksToGenerate.Enqueue(chunk);
                yield break;
            }
        }
    }

    private void GenerateChunksInBackground() {
        if (chunksToGenerate.Count == 0) return;
        if (chunkGeneratorThread != null && chunkGeneratorThread.IsAlive) return;

        chunkGeneratorThread = new Thread(() => {
            while (chunksToGenerate.Count > 0) {
                chunksToGenerate.TryDequeue(out Chunk chunk);
                if (chunk == null) continue;
                chunk.GenerateHeightMap(terrainNoise);
                chunk.Generate();
                chunksToRender.Enqueue(chunk);
            }
        });
        chunkGeneratorThread.Start();
    }
    
    private IEnumerator RenderChunks() {
        // while (chunksToRender.Count > 0) {
        //     chunksToRender.TryDequeue(out Chunk chunk);
        //     chunk.Render();
        //     renderedChunks.Enqueue(chunk);
        //     yield return new WaitForEndOfFrame();
        // }

        Dictionary<Chunk, MeshData> chunkMeshes = new Dictionary<Chunk, MeshData>();
        while (chunksToRender.Count > 0) {
            chunksToRender.TryDequeue(out Chunk chunk);
            chunkMeshes.Add(chunk, chunk.GenerateOptimizedMesh());
            yield return new WaitForEndOfFrame();
        }

        foreach (KeyValuePair<Chunk, MeshData> kvp in chunkMeshes) {
            Chunk chunk = kvp.Key;
            MeshData mesh = kvp.Value;

            chunk.UploadMesh(mesh);
            chunk.Render();
            renderedChunks.Enqueue(chunk);
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Create chunk game object and attach Chunk script.
    /// </summary> 
    /// <returns>Chunk container for the given coordinate or null if one already exists</returns>
    /// <param name="chunkX">X position of the chunk</param>
    /// <param name="chunkZ">Z position of the chunk</param>
    /// <param name="force">Force creation of chunk even if one already exists</param>
    private Chunk CreateChunk(int chunkX, int chunkZ, bool force = false) {
        if (!force && ChunkExists(chunkX, chunkZ)) return null;

        Chunk chunk = new GameObject($"Chunk {chunkX}, {chunkZ}").AddComponent<Chunk>();
        chunk.SetPosition(chunkX, chunkZ);
        chunk.SetParent(transform);
        chunk.SetMaterial(worldMaterial);
        chunk.Initialize();

        return chunk;
    }

    private bool ChunkExists(int chunkX, int chunkZ) {
        if (renderedChunks.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return true;
        if (chunksToGenerate.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return true;
        if (chunksToRender.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return true;
        if (chunksOverflowBuffer.Any(chunk => chunk.GetPosition() == new Vector2Int(chunkX, chunkZ))) return true;
        return false;
    }

    /// <summary>
    /// Render chunk ans add it to the queue of generated chunks.
    /// </summary>
    /// <param name="chunk">Chunk to render</param>
    private void RenderChunk(Chunk chunk) {
        if (chunk == null) return; // Chunk not generated yet
        chunk.Render();
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
        const int CHUNK_SIZE = 16;
        return new Vector2Int(Mathf.FloorToInt(position.x / CHUNK_SIZE), Mathf.FloorToInt(position.z / CHUNK_SIZE));
    }

    private void UpdateInspector() {
        chunksInspector = renderedChunks.Select(chunk => new Vector2(chunk.GetPosition().x, chunk.GetPosition().y)).ToList();
        blockCount = renderedChunks.Sum(chunk => chunk.GetBlockCount());
        TotalVertices = renderedChunks.Sum(chunk => chunk.GetVertexCount());
    }


    #region demos
    private void GenerateTerrain() {
        const int CHUNK_SIZE = 16;
        const int SEA_LEVEL = 60;
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
        const int CHUNK_SIZE = 16;
        const int SEA_LEVEL = 60;

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
        for (int i = 0; i < y; i++) {
            PlaceBlock(x, i, z);
        }
    }
    #endregion
}
