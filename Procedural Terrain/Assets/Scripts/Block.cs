using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour {
    public Vector3Int position { get; private set; }
    
    private GameObject block;
    
    public void SetPosition(int x, int y, int z) {
        position = new Vector3Int(x, y, z);
    }

    public void SetParent(Transform parent) {
        transform.parent = parent;
    }

    public void Render() {
        GameObject stone = Resources.Load("Blocks/StoneBlock") as GameObject;

        block = Instantiate(stone, position, Quaternion.identity);
        block.name = $"Stone block";
        block.transform.parent = transform;
    }

    public void Destroy() {
        Destroy(block);
    }
}
