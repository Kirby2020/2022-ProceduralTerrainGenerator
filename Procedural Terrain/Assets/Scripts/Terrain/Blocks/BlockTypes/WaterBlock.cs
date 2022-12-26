using UnityEngine;

public class WaterBlock : Block {
    public WaterBlock() {
        Type = BlockType.Water;
        Color = Color.blue;
        IsSolid = false;
        IsTransparent = true;
    }
}
