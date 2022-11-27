using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {
    private FractalNoise terrainNoise; // Main noise map for terrain height
    private const int MAX_HEIGHT = 20;
    private const int MIN_HEIGHT = 0;

    private int seaLevel = 10;
    private int maxX = 50;
    private int maxZ = 50;

    private void Awake() {
        terrainNoise = ScriptableObject.CreateInstance<FractalNoise>();
        GenerateTerrain();
    }

    private void GenerateTerrain() {
        for (int x = 0; x < maxX; x++) {
            for (int z = 0; z < maxZ; z++) {
                //int y = Mathf.FloorToInt((float)terrainNoise.Noise(x, z) * MAX_HEIGHT);
                for (int y = 0; y < MAX_HEIGHT; y++) {
                    PlaceBlock(x, y, z);
                }
            }
        }
    }

    private void PlaceBlock(int x, int y, int z) {
        // Dont place blocks under min height or above max height
        if (y < MIN_HEIGHT || y > MAX_HEIGHT) return;
        GameObject block;

        block = Resources.Load("Blocks/StoneBlock") as GameObject;

        var placedBlock = Instantiate(block, new Vector3(x, y, z), Quaternion.identity);
        placedBlock.name = $"Block ({x},\t{y},\t{z})\t";
        placedBlock.transform.parent = transform;
    }
}
