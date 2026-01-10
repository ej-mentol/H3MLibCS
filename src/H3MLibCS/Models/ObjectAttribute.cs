namespace H3M.Models;

public class ObjectAttribute
{
    public string Def { get; set; } = string.Empty;
    public byte[] Passable { get; set; } = new byte[6];
    public byte[] Active { get; set; } = new byte[6];
    public ushort AllowedLandscapes { get; set; }
    public ushort LandscapeGroup { get; set; }
    public uint ObjectClass { get; set; }
    public uint ObjectNumber { get; set; }
    public byte ObjectGroup { get; set; }
    public byte Above { get; set; }
}
