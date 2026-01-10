namespace H3M.Models;

public class MapObject
{
    public Position Position { get; set; }
    public uint TemplateIndex { get; set; }
    public ObjectAttribute? Template { get; set; }
    
    // Additional skip amount if parsing not fully implemented
    // public byte[]? RawData { get; set; } 
}
