using H3M.Models;

namespace H3M.Exporters;

public class ExportOptions
{
    public MapSections IncludedSections { get; set; } = MapSections.Full;

    public Predicate<MapObject>? ObjectFilter { get; set; }

    public void ExcludeUnderground()
    {
        IncludedSections &= ~MapSections.Underground;
        ObjectFilter = obj => obj.Position.Z == 0;
    }
}
