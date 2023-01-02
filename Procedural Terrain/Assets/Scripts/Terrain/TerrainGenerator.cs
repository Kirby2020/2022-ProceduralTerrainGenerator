using System.Threading;
using System.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.Collections.Concurrent;

public class TerrainGenerator : MonoBehaviour {
    private const int RENDER_DISTANCE = 6;     // How many chunks to render around player

    [SerializeField] private Transform player;
    [SerializeField] private Material worldMaterial;

    [SerializeField] private List<Vector2> chunksInspector = new List<Vector2>();  // List of chunks that have been generated
    [SerializeField] private int blockCount = 0;
    [SerializeField] private int TotalVertices = 0;

    private ConcurrentQueue<Chunk> chunksToGenerate = new ConcurrentQueue<Chunk>(); // Queue of chunks to generate
    private ConcurrentQueue<Chunk> chunksToRender = new ConcurrentQueue<Chunk>();   // Queue of chunks to render
    private ConcurrentQueue<Chunk> chunksOverflowBuffer = new ConcurrentQueue<Chunk>();   // Overflow buffer for chunks
    private ConcurrentQueue<Chunk> renderedChunks = new ConcurrentQueue<Chunk>();  // Queue of chunks that have been generated

    private ConcurrentDictionary<Vector2Int, Chunk> chunks = new ConcurrentDictionary<Vector2Int, Chunk>();  // Dictionary of chunks with their position as key

    private FractalNoise terrainNoise;  // Main noise map for terrain height
    private TerrainNoise terrainNoiseTest; 

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
        // test thread
        if (Input.GetKeyDown(KeyCode.Space)) {
            chunkCreatorThread = new Thread(() => {
                for (int i = 0; i < 1000; i++) {
                    // do a heavy calculation
                    int result = Mathf.FloorToInt(Mathf.Sqrt(i) * Mathf.Sqrt(i)) * Mathf.FloorToInt(Mathf.Sqrt(i) * Mathf.Sqrt(i));
                    Debug.Log("Thread 1: " + result);
                }
            });
        }
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
        while (true) {
            List<Vector2Int> emptyChunkPositions = GetEmptyChunkPositions();

            Profiler.BeginSample("Creating chunks");
            StartCoroutine(CreateChunks(emptyChunkPositions));
            
            Profiler.EndSample();
            Profiler.BeginSample("Generating chunks");

            // StartCoroutine(GenerateChunks());
            GenerateChunksInBackground();

            Profiler.EndSample();
            // Profiler.BeginSample("Rendering chunks");

            // // StartCoroutine(RenderChunks());

            // Profiler.EndSample();

            yield return new WaitForEndOfFrame();
        }
    }

    private List<Vector2Int> GetEmptyChunkPositions() {
        int min = -RENDER_DISTANCE / 2;  
        int max = RENDER_DISTANCE / 2;

        Vector2Int playerChunk = GetChunkPosition(player.position);      

        // Get all chunks that need to be created
        ConcurrentDictionary<Vector2Int, int> chunkPositions = new ConcurrentDictionary<Vector2Int, int>(); // <position, distance>

        Parallel.For(min, max, chunkX => {
            Parallel.For(min, max, chunkZ => {
                Vector2Int chunkPosition = new Vector2Int(playerChunk.x + chunkX, playerChunk.y + chunkZ);
                if (ChunkExistsAtPosition(chunkPosition.x, chunkPosition.y)) return;
                int chunkDistance = math.abs(chunkX) + math.abs(chunkZ);
                chunkPositions.TryAdd(chunkPosition, chunkDistance);
            });
        });

        return new List<Vector2Int>(chunkPositions.OrderBy(x => x.Value).Select(x => x.Key));
    }

    /// <summary>
    /// Creates chunks around player
    /// </summary>
    private IEnumerator CreateChunks(List<Vector2Int> chunkPositions) {
        Chunk chunk;

        // Load all chunks around player with radius min to max ( = renderDistance )
        // Starting from the chunk closest to the player
        foreach (var position in chunkPositions) {
            chunk = CreateChunk(position.x, position.y, true);
            chunks.TryAdd(position, chunk);

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
        var chunksToGenerate = chunks.Where(x => x.Value.Status == ChunkStatus.Created).Select(x => x.Value);
        if (chunksToGenerate.Count() == 0) return;
        if (chunkGeneratorThread != null && chunkGeneratorThread.IsAlive) return;

        chunkGeneratorThread = new Thread(() => {
            foreach (Chunk chunk in chunksToGenerate) {
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
        if (!force && ChunkExistsAtPosition(chunkX, chunkZ)) return null;

        Chunk chunk = new GameObject($"Chunk {chunkX}, {chunkZ}").AddComponent<Chunk>();
        chunk.SetPosition(chunkX, chunkZ);
        chunk.SetParent(transform);
        chunk.SetMaterial(worldMaterial);
        chunk.Initialize();

        return chunk;
    }

    private bool ChunkExistsAtPosition(int chunkX, int chunkZ) {
        return chunks.Any(chunk => {
            Vector2Int position = chunk.Key;
            return position.x == chunkX && position.y == chunkZ;
        });
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
