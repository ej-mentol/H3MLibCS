using System.Text;
using H3M.Models;

namespace H3M.Exporters;

public class TextMapExporter : IMapExporter
{
    public string FormatName => "text";
    public string FileExtension => ".txt";

    public void Export(Map map, Stream outputStream, ExportOptions options)
    {
        using var writer = new StreamWriter(outputStream, Encoding.UTF8, leaveOpen: true);
        
        writer.WriteLine($"=== H3M Map Export [{DateTime.Now}] ===");

        if (options.IncludedSections.HasFlag(MapSections.Header))
        {
            writer.WriteLine($"Name:        {map.BasicInfo.Name}");
            writer.WriteLine($"Format:      {map.Format}");
            writer.WriteLine($"Size:        {map.BasicInfo.MapSize}");
            writer.WriteLine($"Description: {map.BasicInfo.Description}");
        }

        if (options.IncludedSections.HasFlag(MapSections.Players))
        {
            writer.WriteLine("\n--- Players ---");
            foreach (var p in map.Players.Where(p => p.CanBeHuman || p.CanBeComputer))
                writer.WriteLine($"Player {p.Id}: Town={(p.HasMainTown ? "Yes" : "No")}, Hero={(p.MainHeroName ?? "Random")}");
        }

        if (options.IncludedSections.HasFlag(MapSections.Objects))
        {
            var objects = map.Objects.AsEnumerable();
            if (options.ObjectFilter != null) objects = objects.Where(o => options.ObjectFilter(o));
            else if (!options.IncludedSections.HasFlag(MapSections.Underground)) objects = objects.Where(o => o.Position.Z == 0);

            var list = objects.ToList();
            writer.WriteLine($"\n--- Objects ({list.Count}) ---");
            foreach (var obj in list.Take(100)) 
                writer.WriteLine($"{obj.Position} {obj.Template?.Def ?? "Unknown"} (Idx: {obj.TemplateIndex})");
            
            if (list.Count > 100) writer.WriteLine($"... and {list.Count - 100} more.");
        }

        if (map.UnparsedSuffix != null && map.UnparsedSuffix.Length > 0)
        {
            writer.WriteLine($"\n--- Unparsed Suffix ({map.UnparsedSuffix.Length} bytes) ---");
            string hex = BitConverter.ToString(map.UnparsedSuffix.Take(256).ToArray()).Replace("-", " ");
            writer.WriteLine(hex);
            if (map.UnparsedSuffix.Length > 256) writer.WriteLine("...");
        }

        writer.Flush();
    }
}
