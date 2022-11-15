using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Value noise class with bi-linear interpolation
/// </summary>
public class ValueNoise : INoise {
    private float[,] noiseMap;
    private int seed = 0;
    private int cellSize = 10;
    
    public void GenerateNoiseMap(int x, int y) {
        noiseMap = new float[x, y];
        Random.InitState(seed);
        
        // Generate grid points every cellSize
        for (int i = 0; i < x; i += cellSize) {
            for (int j = 0; j < y; j += cellSize) {
                noiseMap[i, j] = Random.value;
            }
        }
        // Interpolate each value between grid points
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                if (i % cellSize == 0 && j % cellSize == 0) continue;
                noiseMap[i, j] = Interpolate(i, j);
            }
        }
    }

    private float Interpolate(int i, int j) {
        // Get the 4 surrounding grid points coordinates
        int x0 = i - i % cellSize;                          // Left
        int x1 = (x0 + cellSize) % noiseMap.GetLength(0);   // Right
        int y0 = j - j % cellSize;                          // Bottom
        int y1 = (y0 + cellSize) % noiseMap.GetLength(1);   // Top

        // Get the noise value for each corner
        float value00 = noiseMap[x0, y0];   // Bottom left
        float value10 = noiseMap[x1, y0];   // Bottom right
        float value01 = noiseMap[x0, y1];   // Top left
        float value11 = noiseMap[x1, y1];   // Top right

        // Interpolate between the 4 corners
        float distanceX = (i - x0) / (float)cellSize;   // How far i is between the left and right cell (0 = left, 1 = right)
        float distanceY = (j - y0) / (float)cellSize;   // How far j is between the bottom and top cell (0 = bottom, 1 = top)

        float value0 = Mathf.Lerp(value00, value10, distanceX);    // Interpolate the bottom corners
        float value1 = Mathf.Lerp(value01, value11, distanceX);    // Interpolate the top corners
        float value = Mathf.Lerp(value0, value1, distanceY);       // Interpolate vertically

        return value;
    }

    public float GetNoiseValue(int x, int y) {
        return noiseMap[x, y];
    }

    public void SetSeed(int seed) {
        this.seed = seed;
    }

    public void SetScale(int scale) {
        this.cellSize = scale;
    }
}
