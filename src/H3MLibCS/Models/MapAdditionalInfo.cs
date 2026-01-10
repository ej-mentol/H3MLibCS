namespace H3M.Models;

public class MapAdditionalInfo
{
    public VictoryCondition VictoryCondition { get; set; } = new();
    public LossCondition LossCondition { get; set; } = new();
    public byte[] Teams { get; set; } = new byte[8];
    
    public byte[] AvailableHeroes { get; set; } = Array.Empty<byte>();
    public byte[] AvailableArtifacts { get; set; } = Array.Empty<byte>(); // AB/SoD
    public byte[] AvailableSpells { get; set; } = Array.Empty<byte>();   // SoD
    public byte[] AvailableSkills { get; set; } = Array.Empty<byte>();   // SoD
}
