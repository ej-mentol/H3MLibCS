namespace H3M.Exporters;

public static class ExporterRegistry
{
    private static readonly Dictionary<string, IMapExporter> _exporters = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(IMapExporter exporter)
    {
        if (_exporters.ContainsKey(exporter.FormatName))
        {
            // Or log warning / overwrite
            _exporters[exporter.FormatName] = exporter;
        }
        else
        {
            _exporters.Add(exporter.FormatName, exporter);
        }
    }

    public static IMapExporter? Get(string formatName)
    {
        return _exporters.TryGetValue(formatName, out var exporter) ? exporter : null;
    }

    public static IEnumerable<IMapExporter> GetAll() => _exporters.Values;
}
