using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class TerrainNoise {
    public bool Ready { get; private set; } = false;
    private int seed;
    private Continentalness continentalnessMap;
    private Erosion erosionMap;
    private FractalNoise moistureMap;
    private FractalNoise temperatureMap;

    public TerrainNoise(int seed) {
        this.seed = seed;
        SetupTerrainNoise();
    }

    private async void SetupTerrainNoise() {
        await SetupContinentalness();
        // await SetupErosion();

        continentalnessMap.SetSeed(seed);
        // erosionMap.SetSeed(seed);

        await Task.Run(() => {
            continentalnessMap.Generate();
            // erosionMap.Generate();
        });

        Ready = true;
    }

    private Task SetupContinentalness() {
        return Task.Run(() => {
            continentalnessMap = new Continentalness(8192);
        });
    }

    private Task SetupErosion() {
        return Task.Run(() => {
            erosionMap = new Erosion(8192);
        });
    }

    public int GetHeight(int x, int y) {
        int height = 0;
        int continentalness = continentalnessMap.GetContinentalness(x, y);
        // int erosion = erosionMap.GetErosion(x, y);

        height = continentalness;
        // height -= erosion;

        return height;
    }
}
