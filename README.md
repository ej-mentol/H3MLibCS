# H3MLibCS

A .NET 9 library for parsing Heroes of Might and Magic III map files (.h3m).
Note: This library is better for quick exports than for precise map editing.

## Overview

H3MLibCS is a binary parser focused on resilience and deep data extraction. Unlike basic parsers, it interprets object internal data and provides geometric passability information, making it ideal for AI and tool developers.

## Key Features

- **Deep Object Parsing**: Extracts internal data for Monsters, Resources, Pandora's Boxes, Towns, and Heroes (quantities, guards, messages, etc.).
- **Geometric Analysis**: Provides 6x8 passability and interaction matrices for all objects (CCC/CICC logic), allowing precise map traversal analysis.
- **Structural Resilience**: Uses a dual-layer recovery mechanism:
    - **Object Resync**: Recovers from unknown object structures.
    - **Structural Resync**: Signature-based scanning to find the object block even if the map header is misaligned or corrupted.
- **ML Ready**: Includes a high-performance `MlTensorExporter` that generates normalized 14-channel tensors [14, 144, 144] for neural network training.
- **Memory Safety**: Hard limits on string lengths and data blocks to prevent OOM on malicious or corrupted files.

## Support Matrix

| Format | Support Level | Notes |
| :--- | :--- | :--- |
| **RoE** | Full | Restoration of Erathia |
| **AB / SoD** | Full | Armageddon's Blade / Shadow of Death |
| **HotA** | **Partial** | Basic support for 1.7+ (Parses HotA headers and new object classes) |

## Technical Details

- **Target Framework**: .NET 9.0
- **Encoding**: Windows-1251 (standard for HoMM3 strings).
- **Format**: Reads both GZipped and raw .h3m files automatically.

## Usage & Pitfalls



```csharp

using H3M.IO;

using H3M.Models;



var reader = new MapReader();

using var stream = File.OpenRead("map.h3m");

var map = reader.Read(stream);



foreach (var obj in map.Objects) 

{

    // PITFALL 1: Template can be null if the map is corrupted or uses unknown mods

    if (obj.Template == null) continue;



    Console.WriteLine($"Object: {obj.Template.Def} at ({obj.Position.X}, {obj.Position.Y})");



    // PITFALL 2: H3M coordinates (X, Y) point to the BOTTOM-RIGHT corner (entrance)

    // To map the 8x6 mask to global coordinates, use this logic:

    bool[,] passability = obj.Template.GetPassabilityMatrix();

    for (int row = 0; row < 6; row++)    // Top to Bottom

    {

        for (int col = 0; j < 8; col++)  // Left to Right

        {

            int globalX = obj.Position.X - 7 + col;

            int globalY = obj.Position.Y - 5 + row;

            bool isPassable = passability[row, col];

        }

    }



    // PITFALL 3: Data fields are Nullable. They only exist for specific object types.

    if (obj.Quantity.HasValue) 

        Console.WriteLine($"  Value: {obj.Quantity.Value}"); // Gold in chest, resources in pile, etc.



    if (obj.Guards != null && obj.Guards.Count > 0)

        Console.WriteLine($"  Guards: {obj.Guards.Count} stacks");

}

```



### Why it might break:

- **Custom Map Editors:** Some non-standard editors (like old Unleashed versions) produce headers that violate the official spec. While `Structural Resync` handles most cases, some objects might still be lost.

- **Deep HotA Logic:** While we parse basic HotA objects, deep settings (like specific rewards in new Banks) are currently stored in `map.UnparsedSuffix`.

- **Coordinate Overflow:** For objects placed at the very edge of the map (X < 7 or Y < 5), the global coordinate math shown above can go negative. Always check bounds.
