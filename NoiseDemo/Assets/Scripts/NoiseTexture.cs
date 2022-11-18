using UnityEngine;
using UnityEngine.UI;

/// <summary> 
/// 
/// This class is used to generate a texture from a noise map
/// The type of noise can be chosen in the inspector
/// 
/// </summary>
public class NoiseTexture : MonoBehaviour {
    [SerializeField] private RawImage noisePreview;
    [SerializeField] private Vector2 previewSize;

    [SerializeField] private Vector2Int textureSize;

    [SerializeField] private NoiseType noiseGeneratorType;
    [SerializeField, SerializeReference] private INoise noiseGenerator;
    [SerializeField, Range(0, 1000)] private int seed = 0;
    [SerializeField, Range(1, 100)] private int noiseScale = 1;
    
    private void Awake() {
        textureSize = new Vector2Int(100, 100);
        previewSize = new Vector2(800, 800);
    }

    private void Start() {
        noisePreview = GetComponent<RawImage>();
        SetNoiseGenerator();
    }

    private void FixedUpdate() {
        noisePreview.GetComponent<RectTransform>().sizeDelta = previewSize;
        noisePreview.texture = GenerateTexture();
    }

    private Texture2D GenerateTexture() {
        var texture = new Texture2D(textureSize.x, textureSize.y);
        texture.filterMode = FilterMode.Point; // Removes aliasing effects!!

        for (int x = 0; x < texture.width; x++) {
            for (int y = 0; y < texture.height; y++) {
                float noiseValue = noiseGenerator.GetNoiseValue(x, y);
                var pixel = new Color(noiseValue, noiseValue, noiseValue);
                texture.SetPixel(x, y, pixel);
            }
        }
        texture.Apply();

        return texture;
    }

    private void SetNoiseGenerator() {
        switch (noiseGeneratorType) {
            case NoiseType.Random_Noise: noiseGenerator = new RandomNoise(); break;
            case NoiseType.Value_Noise: noiseGenerator = new ValueNoise(); break;
            case NoiseType.Perlin_Noise: noiseGenerator = new PerlinNoise(); break;
            case NoiseType.Simplex_Noise: break;
            default: noiseGenerator = new RandomNoise(); break;
        }

        noiseGenerator.SetSeed(seed);
        noiseGenerator.SetScale(noiseScale);
        noiseGenerator.GenerateNoiseMap(textureSize.x, textureSize.y);
    }

    [ContextMenu("Update noise")]
    private void UpdateNoise() {
        SetNoiseGenerator();
    }
}
