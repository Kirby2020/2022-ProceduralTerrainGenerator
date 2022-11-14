using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Random noise class using the linear congruential generator formula
/// </summary>
public class RandomNoise : INoise {
    private float[,] noiseMap;
    private int seed = 0;

    public float GetNoiseValue(int x, int y) {
        return noiseMap[x, y];
    }

    public void SetSeed(int seed) {
        this.seed = seed;
    }

    public void GenerateNoiseMap(int sizeX, int sizeY) {
        noiseMap = new float[sizeX, sizeY];
        Random.InitState(seed);

        for (int x = 0; x < sizeX; x++) {
            for (int y = 0; y < sizeY; y++) {
                noiseMap[x, y] = Random.value;
            }
        }
    }
}
