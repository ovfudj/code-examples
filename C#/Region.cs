using Godot;
using System;
using Godotvoxelscavenger.Voxel;
using Godotvoxelscavenger.Voxel.World;

public class Region
{
    public VoxelChunk[] Chunks = new VoxelChunk[VoxelSettings.RegionSize * VoxelSettings.RegionSize * VoxelSettings.RegionSize];

    public VoxelChunk this[Vector3i c]
    {
        get 
        {
            return this[c.x, c.y, c.z];
        }
        set 
        {
            this[c.x, c.y, c.z] = value;
        }
    }
    
    public VoxelChunk this[int x, int y, int z]
    {
        get
        {
            int row = z * VoxelSettings.RegionSize;
            int column = y * VoxelSettings.RegionSizeSquared;
            return Chunks[x + column + row];
        }
        set
        {
            int row = z * VoxelSettings.RegionSize;
            int column = y * VoxelSettings.RegionSizeSquared;
            Chunks[x + column + row] = value;
        }
    }

    public void Each(Action<Vector3i,  VoxelChunk> Action)
    {
        for (int c = 0; c < Chunks.GetLength(0); c++)
        {
            int y = c / VoxelSettings.RegionSizeSquared;
            int z = (c % VoxelSettings.RegionSizeSquared) / VoxelSettings.RegionSize;
            int x = (c % VoxelSettings.RegionSizeSquared) % VoxelSettings.RegionSize;

            Action(new Vector3i(x, y, z), Chunks[c]);
        }
    }

    internal bool InBounds(Vector3i pos)
    {
        return InBounds(pos.x, pos.y, pos.z);
    }

    internal bool InBounds(int x, int y, int z)
    {
        return x >= 0 && y >= 0 && z >= 0 && x < VoxelSettings.RegionSize && y < VoxelSettings.RegionSize && z < VoxelSettings.RegionSize;
    }
}
