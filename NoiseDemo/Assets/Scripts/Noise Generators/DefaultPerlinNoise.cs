using UnityEngine;

public class DefaultPerlinNoise : INoise {
    private float[,] noiseMap;
    private int seed = 0;
    private int scale = 10;

    public void GenerateNoiseMap(int x, int y) {
        noiseMap = new float[x, y];
        // Generate noise values for each coordinate
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                noiseMap[i, j] = Mathf.PerlinNoise((float)i / scale, (float)j / scale);
            }
        }    
    }

    public float GetNoiseValue(int x, int y) {
        return noiseMap[x, y];
    }

    public void SetScale(int scale) {
        this.scale = scale;
    }

    public void SetSeed(int seed) {
        this.seed = seed;
    }
}
