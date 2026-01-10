using H3M.Models;

namespace H3M.Exporters;

public interface IMapExporter
{
    /// <summary>
    /// Unique identifier of the format (e.g., "json", "xml").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Default file extension including dot (e.g., ".json").
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Exports the map to the specified stream with options.
    /// </summary>
    void Export(Map map, Stream outputStream, ExportOptions options);
}
