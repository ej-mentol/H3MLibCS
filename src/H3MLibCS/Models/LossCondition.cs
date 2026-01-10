namespace H3M.Models;

public enum LossConditionType : byte
{
    LoseTown = 0x00,
    LoseHero = 0x01,
    TimeExpires = 0x02,
    None = 0xFF
}

public class LossCondition
{
    public LossConditionType Type { get; set; } = LossConditionType.None;
    public Position? Position { get; set; }
    public ushort? Days { get; set; }
}
