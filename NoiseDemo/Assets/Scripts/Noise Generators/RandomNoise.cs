using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Random noise class using the linear congruential generator formula
/// </summary>
public class RandomNoise : INoise {
    private float[,] noiseMap;
    private int seed = 0;
    private int scale = 1;

    public float GetNoiseValue(int x, int y) {
        return noiseMap[x, y];
    }

    public void SetSeed(int seed) {
        this.seed = seed;
    }

    public void SetScale(int scale) {
        this.scale = scale;
    }

    public void GenerateNoiseMap(int sizeX, int sizeY) {
        noiseMap = new float[sizeX, sizeY];
        Random.InitState(seed);

        for (int x = 0; x < sizeX; x += scale) {
            for (int y = 0; y < sizeY; y += scale) {
                noiseMap[x, y] = Random.value;
            }
        }
    }
}
