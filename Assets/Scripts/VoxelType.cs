using UnityEngine;

public class VoxelType // To struct, pass by ref instead?
{
    public static readonly VoxelType[] Types = new VoxelType[256];
    public static readonly VoxelType Empty = new(0, Color.clear); // Voxel is empty
    public static readonly VoxelType Full = new(1, Color.clear); // Voxel has children (is full)
    public static readonly VoxelType DebugClear = new(2, Color.clear); // Voxel is Clear
    public static readonly VoxelType DebugBlack = new(3, Color.black); // Voxel is Black
    public static readonly VoxelType DebugWhite = new(4, Color.white); // Voxel is White

    public readonly byte Id; // Can remove
    public readonly Color Color;

    private VoxelType(byte id, Color32 color) {
        Id = id;
        Color = color;
        Types[id] = this;
    }
}
