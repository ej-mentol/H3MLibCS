namespace H3M.Models;

public class Map
{
    public MapFormat Format { get; set; }
    public HotAHeader? HotAData { get; set; }
    public BasicInfo BasicInfo { get; set; } = new();
    public List<Player> Players { get; set; } = new();
    public MapAdditionalInfo AdditionalInfo { get; set; } = new();
    public Tile[] Tiles { get; set; } = Array.Empty<Tile>();
    public List<ObjectAttribute> ObjectAttributes { get; set; } = new();
    public List<MapObject> Objects { get; set; } = new();
    public byte[]? UnparsedSuffix { get; set; }
}
