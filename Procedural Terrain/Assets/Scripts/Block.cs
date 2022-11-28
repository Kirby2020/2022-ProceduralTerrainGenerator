using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : ScriptableObject {
    public Vector3Int position { get; private set; }
    public Transform blockContainer { get; set; }
    
    public void SetPosition(int x, int y, int z) {
        position = new Vector3Int(x, y, z);
    }

    public void SetParent(Transform parent) {
        blockContainer = parent;
    }

    public void Place() {
        GameObject block;
        block = Resources.Load("Blocks/StoneBlock") as GameObject;

        var placedBlock = Instantiate(block, position, Quaternion.identity);
        placedBlock.name = $"Block ({position.x},\t{position.y},\t{position.z})\t";
        placedBlock.transform.parent = blockContainer;
    }

    public void Destroy() {
        Destroy(this);
    }
}
