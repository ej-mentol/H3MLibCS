using H3M.Models;

namespace H3M.Exporters;

public class MlTensorExporter : IMapExporter
{
    public string FormatName => "ml-tensor";
    public string FileExtension => ".bin";

    private const int TargetSize = 144; // Fixed size for ML input
    private const int Channels = 7;    // 0:Terrain, 1:Class, 2:Subtype, 3:Road/River, 4:Passable, 5:Value, 6:Entrance

    public void Export(Map map, Stream outputStream, ExportOptions options)
    {
        int actualSize = (int)map.BasicInfo.MapSize;
        int levels = map.BasicInfo.HasTwoLevels ? 2 : 1;

        using var writer = new BinaryWriter(outputStream);

        writer.Write("H3MT"u8); 
        writer.Write((uint)1); 
        writer.Write((uint)TargetSize);
        writer.Write((uint)levels);
        writer.Write((uint)Channels);

        for (int z = 0; z < levels; z++)
        {
            var passabilityGrid = new short[TargetSize, TargetSize];
            var entranceGrid = new short[TargetSize, TargetSize];
            var valueGrid = new short[TargetSize, TargetSize];
            var classGrid = new short[TargetSize, TargetSize];
            var subtypeGrid = new short[TargetSize, TargetSize];

            for (int y = 0; y < Math.Min(actualSize, TargetSize); y++)
            {
                for (int x = 0; x < Math.Min(actualSize, TargetSize); x++)
                {
                    int tileIdx = (z * actualSize * actualSize) + (y * actualSize) + x;
                    var tile = map.Tiles.Length > tileIdx ? map.Tiles[tileIdx] : new Tile();
                    passabilityGrid[x, y] = (short)(tile.TerrainType == 8 || tile.TerrainType == 9 ? 0 : 1);
                }
            }

            foreach (var obj in map.Objects.Where(o => o.Position.Z == z))
            {
                if (obj.Template == null) continue;

                int ox = obj.Position.X;
                int oy = obj.Position.Y;

                // In H3M, (ox, oy) is the BOTTOM-RIGHT corner of the 8x6 template.
                for (int dx = 0; dx < 6; dx++)
                {
                    for (int dy = 0; dy < 8; dy++)
                    {
                        int tx = ox - 7 + dy;
                        int ty = oy - 5 + dx;

                        if (tx < 0 || tx >= TargetSize || ty < 0 || ty >= TargetSize) continue;

                        // Channel 4: Passability (0 = blocked)
                        if ((obj.Template.Passable[dx] & (1 << dy)) == 0)
                        {
                            passabilityGrid[tx, ty] = 0;
                        }

                        // Channel 6: Entrance (1 = active/yellow square)
                        if ((obj.Template.Active[dx] & (1 << dy)) != 0)
                        {
                            entranceGrid[tx, ty] = 1;
                        }
                    }
                }

                if (ox < TargetSize && oy < TargetSize)
                {
                    classGrid[ox, oy] = (short)obj.Template.ObjectClass;
                    subtypeGrid[ox, oy] = (short)obj.Template.ObjectNumber;
                    valueGrid[ox, oy] = CalculateDynamicValue(obj);
                }
            }

            for (int y = 0; y < TargetSize; y++)
            {
                for (int x = 0; x < TargetSize; x++)
                {
                    if (x < actualSize && y < actualSize)
                    {
                        int tileIdx = (z * actualSize * actualSize) + (y * actualSize) + x;
                        writer.Write((short)map.Tiles[tileIdx].TerrainType);
                    }
                    else writer.Write((short)255);

                    writer.Write(classGrid[x, y]);
                    writer.Write(subtypeGrid[x, y]);

                    if (x < actualSize && y < actualSize)
                    {
                        int tileIdx = (z * actualSize * actualSize) + (y * actualSize) + x;
                        var tile = map.Tiles[tileIdx];
                        writer.Write((short)((tile.RoadType & 0x0F) | ((tile.RiverType & 0x0F) << 4)));
                    }
                    else writer.Write((short)0);

                    writer.Write(passabilityGrid[x, y]);
                    writer.Write(valueGrid[x, y]);
                    writer.Write(entranceGrid[x, y]);
                }
            }
        }
    }

    private short CalculateDynamicValue(MapObject obj)
    {
        if (obj.Template == null) return 0;
        
        short baseValue = GetObjectValue(obj.Template.ObjectClass);
        
        if (obj.Quantity.HasValue && obj.Quantity > 0)
        {
            baseValue += (short)(Math.Log(obj.Quantity.Value + 1) * 10);
        }

        if (obj.Guards?.Count > 0)
        {
            baseValue += (short)(obj.Guards.Sum(g => (long)g.Count) / 10);
        }

        return baseValue;
    }

    private short GetObjectValue(uint objClass)
    {
        return objClass switch
        {
            2 => 100,  // Artifact
            5 => 500,  // Castle
            8 => 150,  // Creature Generator (Dwelling)
            17 => -50, // Monster (obstacle)
            34 => 30,  // Resource
            51 => 200, // Pandora Box
            93 => 150, // Spell Scroll
            98 => 1000, // Town
            101 => 250, // Utopia/etc
            _ => 0
        };
    }
}