using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuiController : MonoBehaviour {
    [SerializeField] private CameraController playerCamera;
    [SerializeField] private Text debugText;

    // Update is called once per frame
    void Awake() {
        InvokeRepeating("UpdateText", 0, 1f);
    }

    void UpdateText() {
        var postion = playerCamera.transform.position;
        float x = Mathf.Round(postion.x);
        float y = Mathf.Round(postion.y);
        float z = Mathf.Round(postion.z);
        int chunkX = Mathf.FloorToInt(x / 16);
        int chunkZ = Mathf.FloorToInt(z / 16);
        debugText.text = $"X: {x}\tY: {y}\tZ: {z}\n";
        debugText.text += $"Chunk: {chunkX}, {chunkZ}\n";
    }
}
