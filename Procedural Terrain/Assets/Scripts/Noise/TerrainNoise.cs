using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class TerrainNoise {
    private int seed;
    private Continentalness continentalness;
    private FractalNoise erosion;
    private FractalNoise moisture;
    private FractalNoise temperature;

    public TerrainNoise(int seed) {
        this.seed = seed;
        continentalness = new Continentalness(seed, 4096);
    }

    public int GetContinentalness(int x, int y) {
        return continentalness.GetContinentalness(x, y);
    }
}
