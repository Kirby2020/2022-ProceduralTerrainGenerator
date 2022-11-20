/*
 * A speed-improved simplex noise algorithm for 2D, 3D and 4D in Java.
 *
 * Based on example code by Stefan Gustavson (stegu@itn.liu.se).
 * Optimisations by Peter Eastman (peastman@drizzle.stanford.edu).
 * Better rank ordering method for 4D by Stefan Gustavson in 2012.
 *
 * This could be speeded up even further, but it's useful as it is.
 *
 * Version 2012-03-09
 *
 * This code was placed in the public domain by its original author,
 * Stefan Gustavson. You may use it as you see fit, but
 * attribution is appreciated.
 *
 */

using UnityEngine;

public class SimplexNoise: INoise {  // Simplex noise in 2D, 3D and 4D
    private float scale = 1f;
    private int seed = 0;

    public float GetNoiseValue(int x, int y) {
      double coordX = (float)(x * scale) / 1000;
      double coordY = (float)(y * scale) / 1000;

      float noiseValue = (float)NoiseS3D.Noise(coordX, coordY);
      noiseValue = (noiseValue + 1) * 0.5f;
      return noiseValue;
    }

    public void SetSeed(int seed) {
        this.seed = seed;
    }

    public void SetScale(int scale) {
        this.scale = scale;
    }

    public void GenerateNoiseMap(int x, int y) {
        return;
    }
}