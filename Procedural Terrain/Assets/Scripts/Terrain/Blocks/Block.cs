using UnityEngine;

public abstract class Block {
    public Vector3Int Position { get; protected set; }
    public Color Color { get; protected set; }
    public BlockType Type { get; protected set; }
    public bool IsSolid { get; protected set; } = true;
    public bool IsTransparent { get; protected set; } = false;
    
    public void SetPosition(int x, int y, int z) {
        Position = new Vector3Int(x, y, z);
    }
}
