using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.UI;

public class NoiseTexture : MonoBehaviour {
    [SerializeField] private RawImage noisePreview;
    [SerializeField] private Vector2Int textureSize;
    [SerializeField] private Vector2 previewSize;

    private void Awake() {
        textureSize = new Vector2Int(100, 100);
        previewSize = new Vector2(300, 300);
    }

    private void Start() {
        noisePreview = GetComponent<RawImage>();
    }

    private void Update() {
        noisePreview.GetComponent<RectTransform>().sizeDelta = previewSize;
        noisePreview.texture = GenerateTexture();
    }

    private Texture2D GenerateTexture() {
        var texture = new Texture2D(textureSize.x, textureSize.y);

        for (int x = 0; x < texture.width; x++) {
            for (int y = 0; y < texture.height; y++) {
                var pixel = texture.GetPixel(x, y);
                // TODO: use custom noise classes for getting noise value
                pixel = new Color(1, 0.5f, 0.5f);
                texture.SetPixel(x, y, pixel);
            }
        }
        texture.Apply();

        return texture;
    }
}
