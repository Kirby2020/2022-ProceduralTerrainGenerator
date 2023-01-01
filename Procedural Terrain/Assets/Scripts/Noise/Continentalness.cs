using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Continentalness {
    private const int OCTAVES = 4;
    private const float FREQUENCY = 0.005f;
    private const int AMPLITUDE = 1;
    private const float LACUNARITY = 2.0f;
    private const float PERSISTENCE = 0.5f;

    private int[,] continentalnessMap;
    private FractalNoise noise;
    private SplineInterpolator interpolator;
    private int seed;

    public Continentalness(int seed, int size) {
        this.seed = seed;
        InitInterpolator();
        Generate(size);
    }

    private void InitInterpolator() {
        float[] xValues = new float[] { -2, 0, 1, 1.5f, 2 };    // noise values
        float[] yValues = new float[] { -100, 0, 20, 90, 100 };    // continentalness values

        interpolator = new SplineInterpolator(xValues, yValues);
    }

    private void Generate(int size) {
        continentalnessMap = new int[size, size];

        noise = new FractalNoise();
        noise.SetSeed(seed);
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
