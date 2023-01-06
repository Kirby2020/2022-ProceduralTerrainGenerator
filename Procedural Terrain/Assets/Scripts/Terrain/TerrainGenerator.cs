using System.Threading;
using System.Collections;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Profiling;
using System.Collections.Concurrent;
using System;

public class TerrainGenerator : MonoBehaviour {
    private const int RENDER_DISTANCE = 16;     // How many chunks to render around player

    [SerializeField] private Transform player;
    [SerializeField] private Material worldMaterial;

    [SerializeField] private List<Vector2> chunksInspector = new List<Vector2>();  // List of chunks that have been generated
    [SerializeField] private int blockCount = 0;
    [SerializeField] private int TotalVertices = 0;

    private ConcurrentDictionary<Vector2Int, Chunk> chunks = new ConcurrentDictionary<Vector2Int, Chunk>();  // Dictionary of chunks with their position as key
    private bool isGeneratingChunks = false;

    private TerrainNoise terrainNoise; 


    #region Initialization
    private void Awake() {
        SetTerrainNoise();
        // GenerateSpawnChunks();   // Generates spawn chunks

        InvokeRepeating("UpdateInspector", 0, 5);  // Updates inspector every second
    }

    private void Start() {
        StartCoroutine(GeneratePlayerChunks());
    }

    private void SetTerrainNoise() {
        terrainNoise = new TerrainNoise(0);     
    }

    #endregion

    #region Chunk Generation
    private void GenerateSpawnChunks() {
        int min = -RENDER_DISTANCE / 2;
        int max = RENDER_DISTANCE / 2;

        for (int chunkX = min; chunkX < max; chunkX++) {
            for (int chunkZ = min; chunkZ < max; chunkZ++) {
                Chunk chunk = CreateChunk(chunkX, chunkZ);
                chunk.GenerateHeightMap(terrainNoise);
                chunk.Generate();
                chunk.Render();
            }
        }
    }

    private IEnumerator GeneratePlayerChunks() {
        StartCoroutine(CreateChunks());
        StartCoroutine(RenderChunks());

        while (true) {
            Task.Run(() => GenerateChunks());
            yield return new WaitUntil(() => !isGeneratingChunks);
        }
    }

    /// <summary>
    /// Returns a list of all chunks that need to be generated around the player
    /// </summary>
    private List<Vector2Int> GetEmptyChunkPositionsAroundPosition(Vector2Int playerChunk) {
        int min = -RENDER_DISTANCE / 2;  
        int max = RENDER_DISTANCE / 2;
        
        ConcurrentDictionary<Vector2Int, int> chunkPositions = new ConcurrentDictionary<Vector2Int, int>();

        Parallel.For(min, max, chunkX => {
            Parallel.For(min, max, chunkZ => {
                Vector2Int chunkPosition = new Vector2Int(playerChunk.x + chunkX, playerChunk.y + chunkZ);
                if (ChunkExistsAtPosition(chunkPosition.x, chunkPosition.y)) return;
                int chunkDistance = CalculateDistanceBetweenChunks(playerChunk, chunkPosition);
                chunkPositions.TryAdd(chunkPosition, chunkDistance);
            });
        });

        return new List<Vector2Int>(chunkPositions.OrderBy(x => x.Value).Select(x => x.Key));
    }

    /// <summary>
    /// Creates chunks around player
    /// </summary>
    private IEnumerator CreateChunks() {
        Chunk chunk;
        Vector2Int playerChunk;
        IEnumerable<Vector2Int> emptyChunkPositions;

        while (true) {
            Profiler.BeginSample("CreateChunks");
            
            playerChunk = GetChunkPosition(player.position);   
            // Get all chunks that need to be generated around player and wait until the task is done
            emptyChunkPositions = GetEmptyChunkPositionsAroundPosition(playerChunk);

            if (emptyChunkPositions.Count() == 0) {
                // Debug.LogError("No chunks to create");
                yield return new WaitForEndOfFrame();
            }
            // Debug.LogWarning("Chunks to create: " + emptyChunkPositions.Count());

            // Load all chunks around player with radius min to max ( = renderDistance )
            // Starting from the chunk closest to the player
            foreach (var position in emptyChunkPositions) {
                chunk = CreateChunk(position.x, position.y);
                chunks.TryAdd(position, chunk);
                yield return new WaitForEndOfFrame();
            }

            Profiler.EndSample();
            yield return new WaitForEndOfFrame();
        }
    }    

    private async void GenerateChunks() {
        isGeneratingChunks = true;
        var chunksToGenerate = await Task.Run(() => GetCreatedChunks());

        if (chunksToGenerate.Count() == 0) {
            // Debug.LogError("No chunks to generate");
            isGeneratingChunks = false;
            return;
        }

        // Debug.LogWarning("Chunks to generate: " + chunksToGenerate.Count());

        await Task.Run(() => {
            foreach (Chunk chunk in chunksToGenerate) {
                chunk.GenerateHeightMap(terrainNoise);
                chunk.Generate();
                chunk.GenerateOptimizedMesh();
            }
        });

        isGeneratingChunks = false;
    }
    
    private IEnumerator RenderChunks() {
        IEnumerable<Chunk> chunksToRender;

        while (true) {
            chunksToRender = Task.Run(() => GetGeneratedChunks()).Result;

            if (chunksToRender.Count() == 0) {
                // Debug.LogError("No chunks to render");
                yield return new WaitUntil(() => chunksToRender.Count() > 0);
            }

            // Debug.LogWarning("Chunks to render: " + chunksToRender.Count());

            foreach (Chunk chunk in chunksToRender) {
                try {
                    chunk.UploadMesh();
                } catch (UnityException e) {
                    Debug.LogWarning("Error while rendering chunk: " + e.Message);
                    var chunkPosition = chunks.Where(x => x.Value == chunk).Select(x => x.Key).FirstOrDefault();
                    chunks.TryRemove(chunkPosition, out Chunk chunksToRemove);
                    chunksToRemove.Clear();
                }
                yield return new WaitForEndOfFrame(); // Wait for one frame before rendering next chunk
            }  

            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Create chunk game object and attach Chunk script.
    /// </summary> 
    /// <returns>Chunk container for the given coordinate or null if one already exists</returns>
    /// <param name="chunkX">X position of the chunk</param>
    /// <param name="chunkZ">Z position of the chunk</param>
    private Chunk CreateChunk(int chunkX, int chunkZ) {
        Chunk chunk = new GameObject($"Chunk {chunkX}, {chunkZ}").AddComponent<Chunk>();
        chunk.SetPosition(chunkX, chunkZ);
        chunk.SetParent(transform);
        chunk.SetMaterial(worldMaterial);
        chunk.Initialize();

        return chunk;
    }    

    #endregion

    private IEnumerable<Chunk> GetCreatedChunks() {
        return chunks.Where(x => x.Value.Status == ChunkStatus.Created).Select(x => x.Value);
    }

    private IEnumerable<Chunk> GetGeneratedChunks() {
        return chunks.Where(x => x.Value.Status == ChunkStatus.Generated).Select(x => x.Value);
    }

    private IEnumerable<Chunk> GetRenderedChunks() {
        return chunks.Where(x => x.Value.Status == ChunkStatus.Rendered).Select(x => x.Value);
    }

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

    private int CalculateDistanceBetweenChunks(Vector2Int chunk1, Vector2Int chunk2) {
        return (int)(math.distancesq(chunk1.x, chunk2.x) + math.distancesq(chunk1.y, chunk2.y));
    }

    private async void UpdateInspector() {
        var renderedChunks = await Task.Run(() => GetRenderedChunks());
        chunksInspector = renderedChunks.Select(chunk => new Vector2(chunk.GetPosition().x, chunk.GetPosition().y)).ToList();
        blockCount = renderedChunks.Sum(chunk => chunk.GetBlockCount());
        TotalVertices = renderedChunks.Sum(chunk => chunk.GetVertexCount());
    }

    private void OnDisable() {
        StopAllCoroutines();
    }
}
