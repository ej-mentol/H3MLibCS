namespace H3M.Models;

public class MapObject
{
    public Position Position { get; set; }
    public uint TemplateIndex { get; set; }
    public ObjectAttribute? Template { get; set; }
    
    // Parsed data
    public uint? Owner { get; set; }
    public string? Message { get; set; }
    public List<CreatureStack>? Guards { get; set; }
    
    // Resource / Monster / Treasure specific
    public uint? Quantity { get; set; }
    public uint? Identifier { get; set; } // HotA/SoD ID
    
    // Pandora / Event / Hero specific
    public uint? Experience { get; set; }
    public uint? Mana { get; set; }
    public sbyte? Morale { get; set; }
    public sbyte? Luck { get; set; }
    public uint[]? Resources { get; set; } // 7 resources
    public byte[]? PrimarySkills { get; set; } // 4 skills
    
    // ... we can add more as we implement them
}
