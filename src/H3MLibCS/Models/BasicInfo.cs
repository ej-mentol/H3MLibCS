namespace H3M.Models;

public class BasicInfo
{
    public bool HasHero { get; set; }
    public byte? HotAExtraFlag { get; set; } // HotA specific byte
    public uint MapSize { get; set; }
    public bool HasTwoLevels { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public byte Difficulty { get; set; }
    public byte? MasteryCap { get; set; } // Only for AB/SoD+
    public byte? MaxHeroLevel { get; set; } // HotA 1.7+
}
