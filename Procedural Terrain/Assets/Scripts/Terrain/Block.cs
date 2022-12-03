using UnityEngine;

public class Block : ScriptableObject {
    public Vector3Int Position { get; private set; }
    // public BlockType Type { get; private set; }
    public bool IsSolid { get; private set; } = true;
    
    private GameObject block;
    
    public void SetPosition(int x, int y, int z) {
        Position = new Vector3Int(x, y, z);
    }

    public void SetParent(Transform parent) {
        // transform.parent = parent;
    }

    public void Render() {
        GameObject stone = Resources.Load("Blocks/StoneBlock") as GameObject;

        block = Instantiate(stone, Position, Quaternion.identity);
        block.name = $"Stone block";
        // block.transform.parent = transform;
    }

    public void Destroy() {
        Destroy(block);
    }
}
