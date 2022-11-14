public interface INoise {
    float GetNoiseValue(int x, int y);

    void SetSeed(int seed);

    void GenerateNoiseMap(int x, int y);
}
