using System.Text.Json;
using System.Text.Json.Serialization;
using H3M.Models;

namespace H3M.Exporters;

public class JsonMapExporter : IMapExporter
{
    public string FormatName => "json";
    public string FileExtension => ".json";

    public void Export(Map map, Stream outputStream, ExportOptions options)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        // Create a dynamic object for export, including only requested sections
        var exportData = new Dictionary<string, object>();

        if (options.IncludedSections.HasFlag(MapSections.Header))
            exportData["Format"] = map.Format;

        if (options.IncludedSections.HasFlag(MapSections.Header))
            exportData["BasicInfo"] = map.BasicInfo;

        if (options.IncludedSections.HasFlag(MapSections.Players))
            exportData["Players"] = map.Players;

        if (options.IncludedSections.HasFlag(MapSections.AdditionalInfo))
            exportData["AdditionalInfo"] = map.AdditionalInfo;

        if (options.IncludedSections.HasFlag(MapSections.Tiles))
        {
            // If underground is disabled, filter tiles
            if (!options.IncludedSections.HasFlag(MapSections.Underground))
            {
                int layerSize = (int)(map.BasicInfo.MapSize * map.BasicInfo.MapSize);
                exportData["Tiles"] = map.Tiles.Take(layerSize); // First layer only
            }
            else
            {
                exportData["Tiles"] = map.Tiles;
            }
        }

        if (options.IncludedSections.HasFlag(MapSections.Objects))
        {
            exportData["ObjectAttributes"] = map.ObjectAttributes;

            var objects = map.Objects.AsEnumerable();
            
            // Apply object filter (e.g. Z=0 only)
            if (options.ObjectFilter != null)
                objects = objects.Where(obj => options.ObjectFilter(obj));
            else if (!options.IncludedSections.HasFlag(MapSections.Underground))
                objects = objects.Where(obj => obj.Position.Z == 0);

            exportData["Objects"] = objects.ToList();
        }

        JsonSerializer.Serialize(outputStream, exportData, jsonOptions);
    }
}
