using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    [SerializeField] private Transform player;
    private Queue<Vector2Int> generatedChunks = new Queue<Vector2Int>();  // Queue of chunks that have been generated
    [SerializeField] private List<Vector2> chunksInspector = new List<Vector2>();  // List of chunks that have been generated
    private FractalNoise terrainNoise;  // Main noise map for terrain height
    private const int MAX_HEIGHT = 40;  // Maximum height of terrain
    private const int MIN_HEIGHT = 0;   // Minimum height of terrain
    private const int CHUNK_SIZE = 16;  // Size of each chunk
    private int renderDistance = 8;     // How many chunks to render around player
    private int seaLevel = 40;          // Base terrain height

    private void Awake() {
        SetTerrainNoise();
        generatedChunks = new Queue<Vector2Int>();
        // GenerateTerrain();
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
        int minX = GetPlayerPosition().x - (renderDistance / 2);
        int maxX = minX + renderDistance;
        int minZ = GetPlayerPosition().z - (renderDistance / 2);
        int maxZ = minZ + renderDistance;

        for (int x = minX; x < maxX; x++) {
            for (int z = minZ; z < maxZ; z++) {
                GenerateChunk(x, z);
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
        Vector2Int chunkPos = new Vector2Int(chunkX, chunkZ);
        if (generatedChunks.Contains(chunkPos)) return;

        // Add chunk to queue of generated chunks
        generatedChunks.Enqueue(chunkPos);
        chunksInspector.Add(chunkPos);  // For inspector only

        // Create parent object for chunk
        GameObject chunkContainer = new GameObject($"Chunk ({chunkX},{chunkZ})");
        chunkContainer.transform.parent = transform;

        // Generate chunk
        Chunk chunk = ScriptableObject.CreateInstance<Chunk>();
        chunk.SetPosition(chunkX, chunkZ);
        chunk.SetParent(chunkContainer.transform);
        chunk.Generate(terrainNoise);
    }

    private void UnloadChunks() {
        while (generatedChunks.Count > renderDistance * renderDistance) {
            Vector2Int chunk = generatedChunks.Dequeue();
            DestroyChunk(chunk.x, chunk.y);
        }
    }

    private void DestroyChunk(int chunkX, int chunkZ) {
        int x = chunkX * CHUNK_SIZE; // Get x coordinate of chunk
        int z = chunkZ * CHUNK_SIZE; // Get z coordinate of chunk
        for (int i = x; i < x + CHUNK_SIZE; i++) {
            for (int j = z; j < z + CHUNK_SIZE; j++) {
                GameObject.Find("Chunk (" + chunkX + "," + chunkZ + ")").GetComponent<Chunk>().ClearChunk();
            }
        }
    }
    
    private Vector3Int GetPlayerPosition() {
        return new Vector3Int(Mathf.FloorToInt(player.position.x), Mathf.FloorToInt(player.position.y), Mathf.FloorToInt(player.position.z));
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
