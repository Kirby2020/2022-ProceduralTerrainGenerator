#define CHUNK_SIZE 16
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
#define MIN_HEIGHT 0
#define MAX_HEIGHT 256
#define SEA_LEVEL 64

struct VoxelData {
    static const float3[] voxelVertices = {
    float3(0, 0, 0),//0
    float3(1, 0, 0),//1
    float3(0, 1, 0),//2
    float3(1, 1, 0),//3

    Copy code
        float3(0, 0, 1),//4
        float3(1, 0, 1),//5
        float3(0, 1, 1),//6
        float3(1, 1, 1),//7
    };

    static const int3[] voxelFaceChecks = {
        int3(0, 0, -1),//back
        int3(0, 0, 1),//front
        int3(-1, 0, 0),//left
        int3(1, 0, 0),//right
        int3(0, -1, 0),//bottom
        int3(0, 1, 0)//top
    };

    static const int[6, 4] voxelVertexIndex = {
        {0, 1, 2, 3},
        {4, 5, 6, 7},
        {4, 0, 6, 2},
        {5, 1, 7, 3},
        {0, 1, 4, 5},
        {2, 3, 6, 7},
    };

    static const float2[] voxelUVs = {
        float2(0, 0),
        float2(0, 1),
        float2(1, 0),
        float2(1, 1)
    };

    static const int[6, 6] voxelTris = {
        {0, 2, 3, 0, 3, 1},
        {0, 1, 2, 1, 3, 2},
        {0, 2, 3, 0, 3, 1},
        {0, 1, 2, 1, 3, 2},
        {0, 1, 2, 1, 3, 2},
        {0, 2, 3, 0, 3, 1},
    };
};
