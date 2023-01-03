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
    private const int RENDER_DISTANCE = 4;     // How many chunks to render around player

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

    private void Awake() {
        SetTerrainNoise();
        // GenerateSpawnChunks();   // Generates spawn chunks

        InvokeRepeating("UpdateInspector", 0, 5);  // Updates inspector every second
    }

    private void Start() {
        StartCoroutine(GeneratePlayerChunks());
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

    private IEnumerator GeneratePlayerChunks2() {
        int i = 0;

        while (i < 1) {
            Debug.LogWarning("Generating player chunks: " + i);

            Profiler.BeginSample("Creating chunks");
            Debug.LogWarning("Creating chunks");
            CreateChunks();
            
            Profiler.EndSample();
            Profiler.BeginSample("Generating chunks");
            Debug.LogWarning("Generating chunks");

            // StartCoroutine(GenerateChunks());
            GenerateChunksInBackground();

            Profiler.EndSample();
            Profiler.BeginSample("Rendering chunks");
            Debug.LogWarning("Rendering chunks");
            //RenderChunks();

            Profiler.EndSample();

            i++;

            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator GeneratePlayerChunks() {

        while (true) {
            CreateChunks();
            GenerateChunksInBackground();
            StartCoroutine(RenderChunks());

            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Returns a list of all chunks that need to be generated
    /// </summary>
    private List<Vector2Int> GetEmptyChunkPositions() {
        int min = -RENDER_DISTANCE / 2;  
        int max = RENDER_DISTANCE / 2;

        Vector2Int playerChunk = GetChunkPosition(player.position);      

        ConcurrentDictionary<Vector2Int, int> chunkPositions = new ConcurrentDictionary<Vector2Int, int>(); // <position, distance>

        Parallel.For(min, max, chunkX => {
            Parallel.For(min, max, chunkZ => {
                Vector2Int chunkPosition = new Vector2Int(playerChunk.x + chunkX, playerChunk.y + chunkZ);
                if (ChunkExistsAtPosition(chunkPosition.x, chunkPosition.y)) return;
                Debug.Log("Assigning position: " + chunkPosition);
                int chunkDistance = math.abs(chunkX) + math.abs(chunkZ);
                chunkPositions.TryAdd(chunkPosition, chunkDistance);
            });
        });

        return new List<Vector2Int>(chunkPositions.OrderBy(x => x.Value).Select(x => x.Key));
    }

    /// <summary>
    /// Creates chunks around player
    /// </summary>
    private void CreateChunks() {
        Chunk chunk;
        List<Vector2Int> emptyChunkPositions = GetEmptyChunkPositions();
        if (emptyChunkPositions.Count == 0) {
            Debug.LogError("No chunks to create");
            return;
        }
        Debug.LogWarning("Chunks to create: " + emptyChunkPositions.Count);

        // Load all chunks around player with radius min to max ( = renderDistance )
        // Starting from the chunk closest to the player
        foreach (var position in emptyChunkPositions) {
            Debug.Log("Creating chunk: " + position);
            chunk = CreateChunk(position.x, position.y, true);
            chunks.TryAdd(position, chunk);
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

    private async void GenerateChunksInBackground() {
        var chunksToGenerate = chunks.Where(x => x.Value.Status == ChunkStatus.Created).Select(x => x.Value);

        if (chunksToGenerate.Count() == 0) {
            Debug.LogError("No chunks to generate");
            return;
        }

        Debug.LogWarning("Chunks to generate: " + chunksToGenerate.Count());

        await Task.Run(() => {
            foreach (Chunk chunk in chunksToGenerate) {
                Debug.Log("Generating chunk: " + chunk.GetPosition());
                chunk.GenerateHeightMap(terrainNoise);
                chunk.Generate();
                chunk.GenerateOptimizedMesh();
                chunksToRender.Enqueue(chunk);
            }
        });
    }
    
    private IEnumerator RenderChunks() {
        IEnumerable<Chunk> chunksToRender;

            chunksToRender = chunks.Where(x => x.Value.Status == ChunkStatus.Generated).Select(x => x.Value);

            if (chunksToRender.Count() == 0) {
                Debug.LogError("No chunks to render");
                yield return new WaitUntil(() => chunksToRender.Count() > 0);
            }

            Debug.LogWarning("Chunks to render: " + chunksToRender.Count());

            foreach (Chunk chunk in chunksToRender) {
                Debug.Log("________________Uploading mesh: " + chunk.GetPosition());
                chunk.UploadMesh();
                renderedChunks.Enqueue(chunk);
                yield break;
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

    private Vector2Int GetChunkPosition(Vector3 position) {
        const int CHUNK_SIZE = 16;
        return new Vector2Int(Mathf.FloorToInt(position.x / CHUNK_SIZE), Mathf.FloorToInt(position.z / CHUNK_SIZE));
    }

    private bool ChunkExistsAtPosition(int chunkX, int chunkZ) {
        return chunks.Any(chunk => {
            Vector2Int position = chunk.Key;
            return position.x == chunkX && position.y == chunkZ;
        });
    }

    private void UpdateInspector() {
        var renderedChunks = chunks.Where(x => x.Value.Status == ChunkStatus.Rendered).Select(x => x.Value);
        chunksInspector = renderedChunks.Select(chunk => new Vector2(chunk.GetPosition().x, chunk.GetPosition().y)).ToList();
        blockCount = renderedChunks.Sum(chunk => chunk.GetBlockCount());
        TotalVertices = renderedChunks.Sum(chunk => chunk.GetVertexCount());
    }
}
