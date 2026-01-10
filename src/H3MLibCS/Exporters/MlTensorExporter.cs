using H3M.Models;

namespace H3M.Exporters;

public class MlTensorExporter : IMapExporter
{
    public string FormatName => "ml-tensor";
    public string FileExtension => ".bin"; // Теперь бинарный файл

    public void Export(Map map, Stream outputStream, ExportOptions options)
    {
        int size = (int)map.BasicInfo.MapSize;
        int levels = map.BasicInfo.HasTwoLevels ? 2 : 1;
        const int channels = 6; // 0:Terrain, 1:Class, 2:Subtype, 3:Road/River, 4:Passable, 5:Value

        var objectLookup = map.Objects
            .GroupBy(o => (o.Position.X, o.Position.Y, o.Position.Z))
            .ToDictionary(g => g.Key, g => g.First());

        using var writer = new BinaryWriter(outputStream);

        writer.Write("H3MT"u8); 
        writer.Write((uint)1); 
        writer.Write((uint)size);
        writer.Write((uint)levels);
        writer.Write((uint)channels);

        for (int z = 0; z < levels; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int tileIdx = (z * size * size) + (y * size) + x;
                    var tile = map.Tiles.Length > tileIdx ? map.Tiles[tileIdx] : new Tile();
                    
                    writer.Write((short)tile.TerrainType);

                    objectLookup.TryGetValue(((byte)x, (byte)y, (byte)z), out var obj);

                    uint objClass = obj?.Template?.ObjectClass ?? 0;
                    writer.Write((short)objClass);
                    writer.Write((short)(obj?.Template?.ObjectNumber ?? 0));

                    byte roadRiver = (byte)((tile.RoadType & 0x0F) | ((tile.RiverType & 0x0F) << 4));
                    writer.Write((short)roadRiver);

                    // Channel 4: Passability (1 = walkable, 0 = blocked)
                    // В идеале тут надо смотреть маску проходимости объекта, но пока базово:
                    short passable = 1;
                    if (tile.TerrainType == 8 || tile.TerrainType == 9) passable = 0; // Rock/Lake
                    if (obj != null) passable = 0; 
                    writer.Write(passable);

                    // Channel 5: Strategic Value (Weight)
                    short value = GetObjectValue(objClass);
                    writer.Write(value);
                }
            }
        }
    }

    private short GetObjectValue(uint objClass)
    {
        return objClass switch
        {
            2 => 100,  // Artifact
            5 => 500,  // Castle
            17 => -50, // Monster (obstacle)
            34 => 30,  // Gold
            51 => 200, // Pandora Box
            98 => 1000, // Town
            _ => 0
        };
    }
}
