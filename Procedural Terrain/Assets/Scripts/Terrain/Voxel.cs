using UnityEngine;

/// <summary>
/// Static class that contains data for a voxel.
/// Original code from: https://github.com/pixelreyn/VoxelProjectSeries/tree/Part2-FirstChunk
/// </summary>
public static class Voxel {
    public static readonly Vector3[] voxelVertices = new Vector3[8] {
        new Vector3(0,0,0),//0
        new Vector3(1,0,0),//1
        new Vector3(0,1,0),//2
        new Vector3(1,1,0),//3

        new Vector3(0,0,1),//4
        new Vector3(1,0,1),//5
        new Vector3(0,1,1),//6
        new Vector3(1,1,1),//7
    };

    public static readonly Vector3[] voxelFaceChecks = new Vector3[6] {
        new Vector3(0,0,-1),//back
        new Vector3(0,0,1),//front
        new Vector3(-1,0,0),//left
        new Vector3(1,0,0),//right
        new Vector3(0,-1,0),//bottom
        new Vector3(0,1,0)//top
    };

    public static readonly int[,] voxelVertexIndex = new int[6, 4] {
        {0,1,2,3},
        {4,5,6,7},
        {4,0,6,2},
        {5,1,7,3},
        {0,1,4,5},
        {2,3,6,7},
    };

    public static readonly Vector2[] voxelUVs = new Vector2[4] {
        new Vector2(0,0),
        new Vector2(0,1),
        new Vector2(1,0),
        new Vector2(1,1)
    };

    public static readonly int[,] voxelTris = new int[6, 6] {
        {0,2,3,0,3,1},
        {0,1,2,1,3,2},
        {0,2,3,0,3,1},
        {0,1,2,1,3,2},
        {0,1,2,1,3,2},
        {0,2,3,0,3,1},
    };
}
