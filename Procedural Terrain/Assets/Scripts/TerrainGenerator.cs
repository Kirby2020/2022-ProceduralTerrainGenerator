using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    [SerializeField] private Transform player;
    private FractalNoise terrainNoise; // Main noise map for terrain height
    private const int MAX_HEIGHT = 40;
    private const int MIN_HEIGHT = 0;
    private const int CHUNK_SIZE = 16;
    private int renderDistance = 8;
    private int seaLevel = 40;


    private void Awake() {
        SetTerrainNoise();
        GenerateTerrain();
    }

    private void Update() {
        if (player.position.x > CHUNK_SIZE || player.position.z > CHUNK_SIZE) {
            GenerateTerrain();
        }

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
        GameObject block;
        block = Resources.Load("Blocks/StoneBlock") as GameObject;

        var placedBlock = Instantiate(block, new Vector3(x, y, z), Quaternion.identity);
        placedBlock.name = $"Block ({x},\t{y},\t{z})\t";
        placedBlock.transform.parent = transform;
    }

    private void FillUnderground(int x, int y, int z) {
        for (int i = MIN_HEIGHT; i < y; i++) {
            GameObject block;
            block = Resources.Load("Blocks/StoneBlock") as GameObject;

            var placedBlock = Instantiate(block, new Vector3(x, i, z), Quaternion.identity);
            placedBlock.name = $"Block ({x},\t{i},\t{z})\t";
            placedBlock.transform.parent = transform;
        }
    }
}
