using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Erosion {
    private const int OCTAVES = 1;
    private const int AMPLITUDE = 1;
    private const float FREQUENCY = 0.001f;
    private const float LACUNARITY = 2.0f;
    private const float PERSISTENCE = 0.5f;

    private int[,] erosionMap;
    private FractalNoise noise;
    private SplineInterpolator interpolator;
    private float frequency;
    private int seed = 0;
    private int size = 0;

    public Erosion(int size) {
        noise = new FractalNoise();
        this.size = size;

        InitInterpolator();
    }

    public void SetSeed(int seed) {
        this.seed = seed;
        noise.SetSeed(seed);
    }
    
    private void InitInterpolator() {
        float[] xValues = new float[] { -2, -1, -0.5f, 0, 0.3f, 0.5f, 1, 1.5f, 1.7f, 1.8f, 2};    // noise values
        float[] yValues = new float[] { -100, -80, -20, 0, 5, 10, 30, 70, 90, 97, 100};    // continentalness values

        interpolator = new SplineInterpolator(xValues, yValues);
    }

    public void Generate() {
        erosionMap = new int[size, size];

        noise.Frequency = FREQUENCY;
        noise.Amplitude = AMPLITUDE;
        noise.Octaves = OCTAVES;
        noise.Lacunarity = LACUNARITY;
        noise.Persistence = PERSISTENCE;

        // Generate the continentalness map
        Parallel.For(0, size, x => {
            Parallel.For(0, size, y => {
                float value = (float)(noise.NoiseCombinedOctaves(x, y));
                float interpolatedValue = interpolator.Interpolate(value);
                erosionMap[x, y] = Mathf.FloorToInt(interpolatedValue);             
            });
        });
    }

    public int GetErosion(int x, int y) {
        int size = erosionMap.GetLength(0);
        return erosionMap[x + size / 2, y + size / 2];
    }
}
