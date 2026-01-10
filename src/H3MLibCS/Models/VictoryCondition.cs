namespace H3M.Models;

public enum VictoryConditionType : byte
{
    AcquireArtifact = 0x00,
    AccumulateCreatures = 0x01,
    AccumulateResources = 0x02,
    UpgradeTown = 0x03,
    BuildGrail = 0x04,
    DefeatHero = 0x05,
    CaptureTown = 0x06,
    DefeatMonster = 0x07,
    FlagDwellings = 0x08,
    FlagMines = 0x09,
    TransportArtifact = 0x0A,
    None = 0xFF
}

public class VictoryCondition
{
    public VictoryConditionType Type { get; set; } = VictoryConditionType.None;
    public bool AllowNormalVictory { get; set; }
    public bool AppliesToAI { get; set; }
    
    // Data fields for specific conditions
    public int? ObjectId { get; set; } // ArtifactId, CreatureId etc
    public uint? Quantity { get; set; } // Count
    public Position? Position { get; set; }
    public byte? SecondaryValue { get; set; } // HallLevel, ResourceType etc
}
