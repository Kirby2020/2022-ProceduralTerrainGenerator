using UnityEngine;
using UnityEngine.UI;

/// <summary> 
/// 
/// This class is used to generate a texture from a noise map
/// The type of noise can be chosen in the inspector
/// 
/// </summary>
public class FractalNoiseTexture : MonoBehaviour {
    private FractalNoise fractalNoise;
    [SerializeField] private RawImage noisePreview;
    [SerializeField] private Vector2 previewSize;
    [SerializeField] private Vector2Int textureSize;
    [SerializeField, Range(0, 1000)] private int seed = 0;
    [SerializeField, Range(1, 10)] private int octaves = 2;
    [SerializeField, Range(1, 10)] private int amplitude = 1;
    [SerializeField, Range(1, 10)] private double frequency = 3;
    [SerializeField, Range(0.1f, 10)] private double lacunarity = 2;
    [SerializeField, Range(0.1f, 1)] private double persistence = 0.5;

    
    private void Awake() {
        fractalNoise = new FractalNoise();
        textureSize = new Vector2Int(200, 200);
        previewSize = new Vector2(800, 800);
    }

    private void Start() {
        noisePreview = GetComponent<RawImage>();
    }

    private void FixedUpdate() {
        fractalNoise.Seed = seed;
        fractalNoise.Octaves = octaves;
        fractalNoise.Amplitude = amplitude;
        fractalNoise.Frequency = frequency / 100f;
        fractalNoise.Lacunarity = lacunarity;
        fractalNoise.Persistence = persistence;

        noisePreview.GetComponent<RectTransform>().sizeDelta = previewSize;

        noisePreview.texture = GenerateTexture();
    }

    private Texture2D GenerateTexture() {
        var texture = new Texture2D(textureSize.x, textureSize.y);
        texture.filterMode = FilterMode.Point; // Removes aliasing effects!!

        // for (int x = 0; x < texture.width; x++) {
        //     for (int y = 0; y < texture.height; y++) {
        //         float noiseValue = (float)fractalNoise.NoiseCombinedOctaves(x);
        //         var pixel = new Color(noiseValue, noiseValue, noiseValue);
        //         texture.SetPixel(x, y, pixel);
        //     }
        // }

        for (int x = 0; x < texture.width; x++) {
            float noiseValue = (float)fractalNoise.NoiseCombinedOctaves(x, Time.time * 3);
            float pixelValue = (noiseValue + 1) / 2;
            pixelValue *= textureSize.y;
            texture.SetPixel(x, Mathf.FloorToInt(pixelValue), Color.black);
        }
        texture.Apply();

        return texture;
    }
}
