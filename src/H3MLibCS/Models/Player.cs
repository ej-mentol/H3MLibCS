namespace H3M.Models;

public struct Position
{
    public byte X, Y, Z;
    
    public override string ToString() => $"({X},{Y},{Z})";
}

public class Player
{
    public int Id { get; set; }
    
    public bool CanBeHuman { get; set; }
    public bool CanBeComputer { get; set; }
    public byte Behavior { get; set; }
    
    public byte AllowedTownsBitmask { get; set; }
    public bool AllowedTownConflux { get; set; } // AB/SoD
    
    // SoD-specific
    public byte AllowedAlignments { get; set; } 

    public bool HasRandomTown { get; set; }
    public bool HasMainTown { get; set; }
    
    // If HasMainTown
    public bool MainTownGenerateHero { get; set; }
    public byte MainTownType { get; set; }
    public Position MainTownPosition { get; set; }

    // Hero
    public bool MainHeroIsRandom { get; set; }
    public byte MainHeroType { get; set; }
    public byte? MainHeroFace { get; set; }
    public string? MainHeroName { get; set; }
}
