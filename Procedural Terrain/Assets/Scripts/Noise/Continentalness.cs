using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Continentalness {
    private const int OCTAVES = 3;
    private const float FREQUENCY = 0.0035f;
    private const int AMPLITUDE = 1;
    private const float LACUNARITY = 2.0f;
    private const float PERSISTENCE = 0.5f;

    private int[,] continentalnessMap;
    private FractalNoise noise;
    private SplineInterpolator interpolator;
    private int seed = 0;
    private int size = 0;

    public Continentalness(int size) {
        noise = new FractalNoise();
        this.size = size;

        InitInterpolator();
    }

    public void SetSeed(int seed) {
        this.seed = seed;
        noise.SetSeed(seed);
    }

    private void InitInterpolator() {
        float[] xValues = new float[] { -2, -1, -0.5f, 0, 0.3f, 0.5f, 1, 1.5f, 1.7f, 1.9f, 2};    // noise values
        float[] yValues = new float[] { -100, -80, -20, 0, 5, 10, 30, 70, 90, 97, 100};    // continentalness values

        interpolator = new SplineInterpolator(xValues, yValues);
    }

    public void Generate() {
        continentalnessMap = new int[size, size];

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
                continentalnessMap[x, y] = Mathf.FloorToInt(interpolatedValue);             
            });
        });
    }

    public int GetContinentalness(int x, int y) {
        int size = continentalnessMap.GetLength(0);
        return continentalnessMap[x + size / 2, y + size / 2];
    }
}
