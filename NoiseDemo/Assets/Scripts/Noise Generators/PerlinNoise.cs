using UnityEngine;

/// <summary>
/// C implementation of Perlin noise converted to C#.
/// https://adrianb.io/2014/08/09/perlinnoise.html
/// </summary>
public class PerlinNoise : INoise {
    private float[,] noiseMap;
    private int seed = 0;
    private int scale = 10;
    
    // Hash lookup table as defined by Ken Perlin.  This is a randomly
    // arranged array of all numbers from 0-255 inclusive.
    private readonly int[] permutation = { 151,160,137,91,90,15,                        
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,    
        190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
        129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
        49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    private readonly int[] p;       

    public PerlinNoise() {
        // Doubled permutation to avoid overflow
        p = new int[512];
        for(int x=0;x<512;x++) {
            p[x] = permutation[x%256];
        }
    }                                             

    private Vector2[] gradients = new Vector2[8] {
        new Vector2(1, 0),
        new Vector2(-1, 0),
        new Vector2(0, 1),
        new Vector2(0, -1),
        new Vector2(1, 1).normalized,
        new Vector2(-1, 1).normalized,
        new Vector2(1, -1).normalized,
        new Vector2(-1, -1).normalized
    };

    public void GenerateNoiseMap(int x, int y) {
        noiseMap = new float[x, y];
        // Generate noise values for each coordinate
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                noiseMap[i, j] = PerlinNoise2D((float)i / scale, (float)j / scale);
            }
        }        
    }

    private float PerlinNoise2D(float i, float j) {
        // Get the 4 surrounding grid points coordinates
        int x0 = Mathf.FloorToInt(i - i % scale);                         // Left
        int x1 = (x0 + scale) % noiseMap.GetLength(0);  // Right
        int y0 = Mathf.FloorToInt(j - j % scale);                         // Bottom
        int y1 = (y0 + scale) % noiseMap.GetLength(1);  // Top

        // Get the 4 grid point coordinates
        Vector2Int[] gridPoints = new Vector2Int[4] {
            new Vector2Int(x0, y0),
            new Vector2Int(x1, y0),
            new Vector2Int(x0, y1),
            new Vector2Int(x1, y1)
        };

        // get the distance vector from each grid point to the current point
        Vector2[] distanceVectors = new Vector2[4];
        for (int k = 0; k < 4; k++) {
            distanceVectors[k] = new Vector2(i, j) - gridPoints[k];
        }

        // Get the dot product of the distance vectors and the gradients
        float[] dotProducts = new float[4];
        for (int k = 0; k < 4; k++) {
            dotProducts[k] = Vector2.Dot(distanceVectors[k], gradients[Hash(gridPoints[k].x, gridPoints[k].y) % gradients.Length]);
        }

        // interpolate the dot products
        float xLerp = (i - x0) / (float)scale;
        float yLerp = (j - y0) / (float)scale;

        float top = Mathf.Lerp(dotProducts[0], dotProducts[1], xLerp);
        float bottom = Mathf.Lerp(dotProducts[2], dotProducts[3], xLerp);

        return Mathf.Lerp(top, bottom, yLerp);
    }
    
    // Perlin hash function
    private int Hash(int x, int y) {
        return PERMUTATION[PERMUTATION[x % PERMUTATION.Length] + y % PERMUTATION.Length]; 
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
