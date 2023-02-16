using Godot;
using System.IO;
using System.Text;
using Godotvoxelscavenger.Voxel;

//Custom serializer for chunks so region data can be loaded later

public partial class RegionLoader
{
    string SavePath;

    public RegionLoader(string WorldName)
    {
        SavePath = ProjectSettings.GlobalizePath($"res://Saves/{WorldName}");
        if(!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }
    }

    public void SaveRegionToPosition(Region region, Vector3i position)
    {
        SaveRegionToFile(region,Path.Combine(SavePath,$"{position.x}-{position.y}-{position.z}.vreg"));
    }

    public Region LoadRegionFromPosition(Vector3i position)
    {
        return LoadRegionFromFile(Path.Combine(SavePath,$"{position.x}-{position.y}-{position.z}.vreg"));
    }

    public Region LoadRegionFromFile(string path)
    {
        Region region = new Region();

        if(File.Exists(path))
        {
            using (var stream = File.Open(path, FileMode.Open))
            {
                using (var reader = new BinaryReader(stream,Encoding.UTF8))
                {
                    for(var i = 0; i < region.Chunks.GetLength(0); i ++)
                    {
                        //Is a chunk saved here?
                        if(reader.ReadBoolean())
                        {
                            region.Chunks[i] = new VoxelChunk();
                            LoadChunk(reader,region.Chunks[i]);
                        }
                    }
                    reader.Close();
                }
            }
        }

        return region;
    }

    public void SaveRegionToFile(Region region, string path)
    {
        using (var stream = File.Open(path, FileMode.Create))
        {
            using (var writer = new BinaryWriter(stream,Encoding.UTF8,false))
            {
                for(var i = 0; i < region.Chunks.GetLength(0); i ++)
                {
                    if(region.Chunks[i] == null)
                    {
                        writer.Write(false);
                    }
                    else
                    {
                        writer.Write(true);
                        SaveChunk(writer,region.Chunks[i]);
                    }
                }
                writer.Close();
            }
        }
    }

    public void SaveChunk(BinaryWriter writer, VoxelChunk chunk)
    {
        if(chunk.Empty)
        {
            //Mark to not populate if the chunk is empty
            writer.Write(false);
        }
        else
        {
            //Mark to populate if the chunk is populated
            writer.Write(true);
            for(var i = 0; i < chunk.Voxels.GetLength(0); i ++)
            {
                writer.Write(chunk.Voxels[i]);
            }
        }
    }

    public void LoadChunk(BinaryReader reader, VoxelChunk chunk)
    {
        //Populate the chunk if it isnt empty
        if(reader.ReadBoolean())
        {
            for(var i = 0; i < chunk.Voxels.GetLength(0); i ++)
            {
                chunk.Voxels[i] = reader.ReadUInt16();
            }
            chunk.Empty = false;
        }
    }
}
