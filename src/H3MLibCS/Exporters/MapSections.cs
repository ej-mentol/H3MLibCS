namespace H3M.Exporters;

[Flags]
public enum MapSections
{
    None = 0,
    Header = 1 << 0,
    Players = 1 << 1,
    AdditionalInfo = 1 << 2,
    Tiles = 1 << 3,
    Objects = 1 << 4,
    Underground = 1 << 5, // Special flag to toggle Z=1
    
    // Shortcuts
    BasicInfo = Header | Players,
    Full = Header | Players | AdditionalInfo | Tiles | Objects | Underground
}
