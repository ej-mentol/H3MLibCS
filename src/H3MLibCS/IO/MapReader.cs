using H3M.Models;

namespace H3M.IO;

public class MapReader
{
    public Map Read(Stream stream)
    {
        using var reader = new H3MReader(stream);
        var map = new Map();

        map.Format = (MapFormat)reader.ReadUInt32();
        
        if ((uint)map.Format >= 32) 
        {
            map.HotAData = ReadHotAHeader(reader);
        }
        
        map.BasicInfo = ReadBasicInfo(reader, map.Format);
        ReadPlayers(reader, map);
        ReadAdditionalInfo(reader, map);
        ReadTiles(reader, map);
        ReadObjectAttributes(reader, map);
        ReadObjects(reader, map);

        if (reader.Position < reader.Length)
        {
            long remaining = reader.Length - reader.Position;
            if (remaining > 0x2000000)
            {
                Console.WriteLine($"[Warning] Unparsed suffix too large ({remaining} bytes). Truncating.");
                remaining = 0x2000000;
            }
            map.UnparsedSuffix = reader.ReadBytes((int)remaining);
        }
        return map;
    }

    private HotAHeader ReadHotAHeader(H3MReader reader)
    {
        var header = new HotAHeader {
            Magic = reader.ReadBytes(4),
            Version = reader.ReadUInt32(),
            ScriptingEnabled = reader.ReadBool(),
            Reserved = reader.ReadBytes(23) 
        };

        string magic = System.Text.Encoding.ASCII.GetString(header.Magic);
        if (magic != "HotA")
        {
             Console.WriteLine($"[Warning] Invalid HotA magic at {reader.Position - 32}");
        }
        return header;
    }

    private BasicInfo ReadBasicInfo(H3MReader reader, MapFormat format)
    {
        var bi = new BasicInfo();
        bi.HasHero = reader.ReadBool();
        
        if ((uint)format >= 32) bi.HotAExtraFlag = reader.ReadByte(); 
        
        bi.MapSize = reader.ReadUInt32();
        bi.HasTwoLevels = reader.ReadBool();
        bi.Name = reader.ReadString();
        bi.Description = reader.ReadString();
        bi.Difficulty = reader.ReadByte();
        
        if ((uint)format >= 21) bi.MasteryCap = reader.ReadByte();
        if ((uint)format >= 32) bi.MaxHeroLevel = reader.ReadByte();
        
        return bi;
    }

    private void ReadObjectBody(H3MReader reader, MapObject obj, MapFormat format)
    {
        var metaType = MetaMapper.GetMetaType(obj.Template!.ObjectClass);
        bool isAbsod = (uint)format >= 21;
        
        switch (metaType)
        {
            case MetaType.GenericImpassableTerrain: 
            case MetaType.GenericImpassableTerrainAbSoD: 
                reader.Skip(5); 
                break;

            case MetaType.Monster: 
                if (isAbsod) reader.ReadUInt32(); 
                reader.ReadUInt16(); 
                reader.ReadByte(); 
                if (reader.ReadBool()) 
                { 
                    reader.ReadString(); 
                    reader.Skip((uint)format >= 21 ? 28 : 14); 
                } 
                reader.Skip(4); 
                break;

            case MetaType.Artifact: 
            case MetaType.Resource: 
                if (isAbsod) reader.ReadUInt32();
                if (reader.ReadBool()) 
                { 
                    reader.ReadString(); 
                    if (reader.ReadBool()) reader.Skip((uint)format >= 21 ? 28 : 14); 
                    reader.Skip(4); 
                } 
                if (metaType == MetaType.Resource) 
                { 
                    reader.ReadUInt32(); 
                    reader.Skip(4); 
                } 
                break;

            case MetaType.Town: 
            case MetaType.RandomTown: 
                ReadTown(reader, obj, format); 
                break;

            case MetaType.Hero: 
            case MetaType.RandomHero: 
            case MetaType.Prison: 
                ReadHero(reader, obj, format); 
                break;

            case MetaType.ResourceGenerator: 
            case MetaType.Dwelling: 
            case MetaType.Lighthouse: 
            case MetaType.Shipyard: 
            case MetaType.AbandonedMineAbSoD: 
                reader.ReadUInt32(); 
                break;

            case MetaType.RandomDwellingAbSoD: 
            case MetaType.RandomDwellingPresetAlignmentAbSoD: 
            case MetaType.RandomDwellingPresetLevelAbSoD: 
                reader.ReadUInt32(); 
                if (metaType == MetaType.RandomDwellingPresetAlignmentAbSoD) 
                { 
                    reader.ReadUInt32(); 
                    reader.ReadUInt16(); 
                } 
                else if (metaType == MetaType.RandomDwellingPresetLevelAbSoD) 
                {
                    reader.ReadUInt16(); 
                }
                break;

            default: 
                if (metaType == MetaType.Unknown)
                {
                    Console.WriteLine($"Unknown object class {obj.Template!.ObjectClass} at {obj.Position}");
                    if (reader.Position + 5 <= reader.Length) reader.Skip(5);
                }
                else if (metaType.ToString().StartsWith("Generic")) 
                    reader.Skip(5); 
                break;
        }
    }

    private void ReadObjects(H3MReader reader, Map map)
    {
        uint count = 0;
        try { count = reader.ReadUInt32(); } catch { return; }
        
        if (count > 100000) count = 0; 

        for (int i = 0; i < (int)count; i++)
        {
            if (reader.Position + 7 > reader.Length) break;
            long startPos = reader.Position;
            try
            {
                var obj = new MapObject { 
                    Position = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }, 
                    TemplateIndex = reader.ReadUInt32() 
                };
                
                if (obj.TemplateIndex < map.ObjectAttributes.Count)
                {
                    obj.Template = map.ObjectAttributes[(int)obj.TemplateIndex];
                    ReadObjectBody(reader, obj, map.Format);
                    map.Objects.Add(obj);
                }
                else 
                { 
                    if (!Resync(reader, map)) break; 
                }
            }
            catch 
            { 
                if (!Resync(reader, map)) break; 
            }
        }
    }

    private bool Resync(H3MReader reader, Map map, int maxAttempts = 500)
    {
        long limit = Math.Min(reader.Length, reader.Position + 10000);
        int mapSize = map.BasicInfo.MapSize > 0 ? (int)map.BasicInfo.MapSize : 255;
        int maxIndex = map.ObjectAttributes.Count;
        int attempts = 0;
        
        while (reader.Position < limit - 7 && attempts++ < maxAttempts)
        {
            long current = reader.Position;
            try 
            {
                byte x = reader.ReadByte(); 
                byte y = reader.ReadByte(); 
                byte z = reader.ReadByte(); 
                uint idx = reader.ReadUInt32();

                if (x < mapSize && y < mapSize && z <= 1 && idx < (uint)maxIndex) 
                { 
                    var template = map.ObjectAttributes[(int)idx];
                    if (!string.IsNullOrEmpty(template.Def) && template.Def.Length > 3)
                    {
                        Console.WriteLine($"Resynced at {current}, found object '{template.Def}' at ({x},{y},{z})");
                        reader.Position = current; 
                        return true; 
                    }
                }
            } 
            catch { }
            reader.Position = current + 1;
        }
        return false;
    }

    private void ReadTown(H3MReader reader, MapObject obj, MapFormat format) 
    { 
        if ((uint)format >= 21 && obj.Template!.ObjectClass != 98) reader.ReadUInt32(); 
        reader.ReadUInt32(); 
        if (reader.ReadBool()) reader.ReadString(); 
        if (reader.ReadBool()) reader.Skip((uint)format >= 21 ? 28 : 14); 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.Skip(12); else reader.Skip(1); 
        if ((uint)format >= 21) reader.Skip(18); 
        
        uint eventCount = reader.ReadUInt32(); 
        if (eventCount > 1000) 
        {
            throw new InvalidDataException("Abnormal event count"); 
        }
        
        for(int k=0; k<(int)eventCount; k++) 
        { 
            reader.ReadString();
            reader.ReadString();
            reader.Skip(28); 
            reader.ReadByte();
            if ((uint)format >= 28) reader.ReadByte();
            reader.ReadByte();
            reader.ReadUInt16();
            reader.ReadByte();
            reader.Skip(17);
            reader.Skip(6);
            reader.Skip((uint)format >= 21 ? 28 : 14);
            reader.Skip(4);
        }
        if ((uint)format >= 28) reader.ReadByte();
        reader.Skip(3);
    }

    private void ReadHero(H3MReader reader, MapObject obj, MapFormat format) 
    { 
        if ((uint)format >= 21) reader.ReadUInt32(); 
        reader.ReadUInt32(); 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.ReadString(); 
        if (reader.ReadBool()) reader.ReadUInt32(); 
        if (reader.ReadBool()) reader.ReadByte(); 
        if (reader.ReadBool()) 
        { 
            uint count = reader.ReadUInt32(); 
            if (count < 100) reader.Skip((int)count * 2); 
        } 
        if (reader.ReadBool()) reader.Skip((uint)format >= 21 ? 28 : 14); 
        reader.ReadByte(); 
        if (reader.ReadBool()) 
        { 
            if ((uint)format <= 14) 
            {
                reader.Skip(31); 
            }
            else 
            { 
                reader.Skip(40); 
                uint bpCount = reader.ReadUInt16(); 
                if (bpCount < 100) reader.Skip((int)bpCount * 4); 
            } 
        } 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.ReadString(); 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.Skip((uint)format >= 28 ? 9 : 1); 
        if (reader.ReadBool()) reader.Skip(4); 
        reader.Skip(16); 
    }

    private void ReadObjectAttributes(H3MReader reader, Map map) 
    { 
        uint count = reader.ReadUInt32(); 
        if (count > 5000) return; 
        for (int i = 0; i < count; i++) 
        { 
            var oa = new ObjectAttribute { 
                Def = reader.ReadString(), 
                Passable = reader.ReadBytes(6), 
                Active = reader.ReadBytes(6), 
                AllowedLandscapes = reader.ReadUInt16(), 
                LandscapeGroup = reader.ReadUInt16(), 
                ObjectClass = reader.ReadUInt32(), 
                ObjectNumber = reader.ReadUInt32(), 
                ObjectGroup = reader.ReadByte(), 
                Above = reader.ReadByte() 
            }; 
            reader.Skip(16); 
            map.ObjectAttributes.Add(oa); 
        } 
    }

    private void ReadTiles(H3MReader reader, Map map) 
    { 
        uint size = map.BasicInfo.MapSize; 
        int count = (int)(size * size * (map.BasicInfo.HasTwoLevels ? 2 : 1)); 
        if (count > 0 && count < 1000000) 
        {
            map.Tiles = new Tile[count];
            for (int i = 0; i < count; i++) 
            {
                map.Tiles[i] = new Tile {
                    TerrainType = reader.ReadByte(),
                    TerrainSprite = reader.ReadByte(),
                    RiverType = reader.ReadByte(),
                    RiverSprite = reader.ReadByte(),
                    RoadType = reader.ReadByte(),
                    RoadSprite = reader.ReadByte(),
                    Mirroring = reader.ReadByte()
                };
            }
        }
    }

    private void ReadAdditionalInfo(H3MReader reader, Map map) 
    { 
        var ai = map.AdditionalInfo; 
        ai.VictoryCondition = ReadWinCondition(reader, map.Format); 
        ai.LossCondition = ReadLossCondition(reader);
        byte teamCount = reader.ReadByte(); 
        if (teamCount > 0 && teamCount < 10) ai.Teams = reader.ReadBytes(8); 
        
        uint hCount = (uint)map.Format >= 28 ? 156u : 128u;
        if ((uint)map.Format >= 32) 
        { 
            hCount = reader.ReadUInt32(); 
            if (hCount < 1000) reader.Skip((int)((hCount + 7) / 8)); 
        }
        else 
        { 
            reader.ReadBytes((uint)map.Format >= 21 ? 20 : 16); 
        }
        
        if ((uint)map.Format >= 21) 
        { 
            uint pc = reader.ReadUInt32(); 
            if (pc < 1000) reader.Skip((int)pc); 
        }
        
        if ((uint)map.Format >= 28) ReadCustomHeroes(reader);
        reader.Skip(31);
        
        if ((uint)map.Format > 14) reader.ReadBytes((uint)map.Format >= 28 ? 18 : 17);
        if ((uint)map.Format >= 28) 
        { 
            reader.ReadBytes(9); 
            reader.ReadBytes(4); 
        }
        
        ReadRumors(reader); 
        if ((uint)map.Format >= 28 && hCount < 1000) ReadHeroSettings(reader, (int)hCount); 
    }

    private void ReadCustomHeroes(H3MReader reader) 
    { 
        uint count = reader.ReadByte(); 
        for (int i = 0; i < count; i++) 
        { 
            reader.ReadByte(); 
            reader.ReadByte(); 
            try { reader.ReadString(); } catch { } 
            reader.ReadByte(); 
        } 
    }

    private void ReadRumors(H3MReader reader) 
    { 
        try 
        { 
            uint count = reader.ReadUInt32(); 
            if (count < 100) 
            {
                for (int i = 0; i < count; i++) 
                { 
                    reader.ReadString(); 
                    reader.ReadString(); 
                } 
            }
        } 
        catch { } 
    }

    private void ReadHeroSettings(H3MReader reader, int count) 
    { 
        for (int i = 0; i < count; i++) 
        { 
            if (!reader.ReadBool()) continue; 
            bool hasExp = reader.ReadBool(); 
            bool hasSkills = reader.ReadBool(); 
            bool hasArtifacts = reader.ReadBool(); 
            bool hasBio = reader.ReadBool(); 
            byte gender = reader.ReadByte(); 
            bool hasSpells = reader.ReadBool(); 
            bool hasPrimary = reader.ReadBool(); 
            
            if (hasExp) reader.ReadUInt32(); 
            if (hasSkills) 
            { 
                uint sc = reader.ReadUInt32(); 
                if (sc < 100) reader.Skip((int)sc * 2); 
            } 
            if (hasArtifacts) 
            { 
                reader.Skip(19 * 2); 
                uint bpCount = reader.ReadUInt16(); 
                if (bpCount < 100) reader.Skip((int)bpCount * 2); 
            } 
            if (hasBio) try { reader.ReadString(); } catch { } 
            if (hasSpells) reader.Skip(9); 
            if (hasPrimary) reader.Skip(4); 
        } 
    }

    private VictoryCondition ReadWinCondition(H3MReader reader, MapFormat format) 
    { 
        var vc = new VictoryCondition(); 
        vc.Type = (VictoryConditionType)reader.ReadByte(); 
        if (vc.Type != VictoryConditionType.None) 
        { 
            vc.AllowNormalVictory = reader.ReadBool(); 
            vc.AppliesToAI = reader.ReadBool(); 
            switch (vc.Type) 
            { 
                case VictoryConditionType.AcquireArtifact: 
                    vc.ObjectId = reader.ReadByte(); 
                    if ((uint)format >= 21) reader.ReadByte(); 
                    break; 
                case VictoryConditionType.AccumulateCreatures: 
                    vc.ObjectId = reader.ReadUInt16(); 
                    if ((uint)format >= 21) reader.ReadByte(); 
                    vc.Quantity = reader.ReadUInt32(); 
                    break; 
                case VictoryConditionType.AccumulateResources: 
                    vc.SecondaryValue = reader.ReadByte(); 
                    vc.Quantity = reader.ReadUInt32(); 
                    break; 
                case VictoryConditionType.UpgradeTown: 
                case VictoryConditionType.BuildGrail: 
                case VictoryConditionType.DefeatHero: 
                case VictoryConditionType.CaptureTown: 
                case VictoryConditionType.DefeatMonster: 
                    vc.Position = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; 
                    break; 
                case VictoryConditionType.TransportArtifact: 
                    vc.ObjectId = reader.ReadByte(); 
                    vc.Position = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; 
                    break; 
            } 
        } 
        return vc; 
    }

    private LossCondition ReadLossCondition(H3MReader reader) 
    { 
        var lc = new LossCondition(); 
        lc.Type = (LossConditionType)reader.ReadByte(); 
        if (lc.Type != LossConditionType.None) 
        { 
            switch (lc.Type) 
            { 
                case LossConditionType.LoseTown: 
                case LossConditionType.LoseHero: 
                    lc.Position = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; 
                    break; 
                case LossConditionType.TimeExpires: 
                    lc.Days = reader.ReadUInt16(); 
                    break; 
            } 
        } 
        return lc; 
    }

    private void ReadPlayers(H3MReader reader, Map map) 
    { 
        for (int i = 0; i < 8; i++) 
        { 
            var p = new Player { Id = i }; 
            p.CanBeHuman = reader.ReadBool(); 
            p.CanBeComputer = reader.ReadBool(); 
            p.Behavior = reader.ReadByte(); 
            
            if ((uint)map.Format >= 28) p.AllowedAlignments = reader.ReadByte(); 
            p.AllowedTownsBitmask = reader.ReadByte(); 
            
            if ((uint)map.Format >= 21) p.AllowedTownConflux = reader.ReadBool(); 
            p.HasRandomTown = reader.ReadBool(); 
            p.HasMainTown = reader.ReadBool(); 
            
            if (p.HasMainTown) 
            { 
                if ((uint)map.Format >= 21) 
                { 
                    p.MainTownGenerateHero = reader.ReadBool(); 
                    p.MainTownType = reader.ReadByte(); 
                } 
                p.MainTownPosition = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; 
            } 
            
            p.MainHeroIsRandom = reader.ReadBool(); 
            p.MainHeroType = reader.ReadByte(); 
            
            if ((uint)map.Format <= 14) 
            { 
                if (p.MainHeroType != 0xFF) 
                { 
                    p.MainHeroFace = reader.ReadByte(); 
                    p.MainHeroName = reader.ReadString(); 
                } 
            } 
            else 
            { 
                p.MainHeroFace = reader.ReadByte(); 
                p.MainHeroName = reader.ReadString(); 
            } 
            map.Players.Add(p); 
        } 
    }
}
