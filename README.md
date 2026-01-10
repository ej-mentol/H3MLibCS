# H3MLibCS

A .NET 9 library for parsing Heroes of Might and Magic III map files (.h3m).

## Overview

H3MLibCS is a binary parser designed for stability. It uses a **Resync mechanism** to survive unknown object structures and modded data, making it compatible with a wide range of map versions.

## Support Matrix

| Format | Support Level | Notes |
| :--- | :--- | :--- |
| **RoE** | Full | Restoration of Erathia |
| **AB / SoD** | Full | Armageddon's Blade / Shadow of Death |
| **HotA** | **Partial** | Horn of the Abyss 1.7+ |

## HotA Support Details

The library can open HotA maps without crashing, but it does not fully interpret all new data:

### What is Parsed:
- **Extended Header**: Version, Magic ("HotA"), and Scripting Flag.
- **New Objects**: Recognized via ID, but custom body data is often handled by a fallback.
- **Basic Info**: Name, Description, and the HotA-specific Max Hero Level field.

### What is NOT Parsed (Skipped):
- **Scripts**: HotA-specific binary scripting data and event instructions are not decoded. They are preserved as a raw byte blob in `map.UnparsedSuffix`.
- **Extended Object Properties**: Specific data for new HotA banks, warehouses, and decorations is currently skipped (using Resync or a 5-byte fallback).
- **Town/Hero Ext**: New extended attributes for Factory/Cove specific to HotA are ignored.

## Features

- **Resilience**: Attempts to recover parsing by scanning for the next valid object header if an error occurs.
- **Memory Safety**: Hard limits on string lengths (1MB) and raw data blocks (32MB) to prevent OOM issues.
- **GZip**: Automatic handling of compressed map files.

## Technical Details

- **Target Framework**: .NET 9.0
- **Encoding**: Windows-1251 (standard for HoMM3 strings).
- **Namespace**: All classes are under the `H3M` namespace.

## Usage

```csharp
using H3M.IO;
using H3M.Models;

var reader = new MapReader();
using var stream = File.OpenRead("hota_map.h3m");
var map = reader.Read(stream);

Console.WriteLine($"Map: {map.BasicInfo.Name} ({map.Format})");

// Access unparsed HotA scripts
if (map.UnparsedSuffix != null)
{
    Console.WriteLine($"Raw scripting data: {map.UnparsedSuffix.Length} bytes");
}
```
