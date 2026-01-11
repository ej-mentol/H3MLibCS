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
        
        long posBeforeAttributes = reader.Position;
        ReadObjectAttributes(reader, map);
        ReadObjects(reader, map);

        // Robust Recovery: if no objects found, attempt to resync via signature scanning
        if (map.Objects.Count == 0 && reader.Length > posBeforeAttributes)
        {
            if (TryStructuralResync(reader, map, posBeforeAttributes))
            {
                ReadObjects(reader, map);
            }
        }

        if (reader.Position < reader.Length)
        {
            long remaining = reader.Length - reader.Position;
            if (remaining > 0x2000000) remaining = 0x2000000;
            map.UnparsedSuffix = reader.ReadBytes((int)remaining);
        }
        return map;
    }

    private bool TryStructuralResync(H3MReader reader, Map map, long startSearchPos)
    {
        reader.Position = startSearchPos;
        long limit = Math.Min(reader.Length - 20, startSearchPos + 100000); 
        
        while (reader.Position < limit)
        {
            long current = reader.Position;
            try
            {
                uint count = reader.ReadUInt32();
                if (count > 0 && count < 2000)
                {
                    uint strLen = reader.ReadUInt32();
                    if (strLen > 4 && strLen < 50)
                    {
                        byte[] bytes = reader.ReadBytes((int)strLen);
                        string def = System.Text.Encoding.ASCII.GetString(bytes).ToLower();
                        if (def.EndsWith(".def"))
                        {
                            reader.Position = current;
                            map.ObjectAttributes.Clear();
                            ReadObjectAttributes(reader, map);
                            return true;
                        }
                    }
                }
            }
            catch { }
            reader.Position = current + 1;
        }
        return false;
    }

    private HotAHeader ReadHotAHeader(H3MReader reader)
    {
        return new HotAHeader {
            Magic = reader.ReadBytes(4),
            Version = reader.ReadUInt32(),
            ScriptingEnabled = reader.ReadBool(),
            Reserved = reader.ReadBytes(23) 
        };
    }

    private BasicInfo ReadBasicInfo(H3MReader reader, MapFormat format)
    {
        var bi = new BasicInfo {
            HasHero = reader.ReadBool()
        };
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
            case MetaType.Monster:
                ReadMonster(reader, obj, format);
                break;
            case MetaType.Artifact:
            case MetaType.SpellScroll:
                ReadArtifact(reader, obj, format);
                break;
            case MetaType.Resource:
                ReadResource(reader, obj, format);
                break;
            case MetaType.PandorasBox:
            case MetaType.Event:
                ReadPandoraOrEvent(reader, obj, format);
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
            case MetaType.Sign:
            case MetaType.OceanBottle:
                reader.ReadString(); 
                reader.Skip(4); 
                break;
            case MetaType.Garrison:
            case MetaType.GarrisonAbSoD:
                obj.Owner = reader.ReadUInt32(); 
                ReadCreatureSet(reader, format);
                if (isAbsod) reader.ReadBool(); 
                reader.Skip(8);
                break;
            case MetaType.Scholar:
                reader.ReadByte(); 
                reader.ReadByte(); 
                reader.Skip(6);
                break;
            default:
                if (IsOwnedObject(obj.Template.ObjectClass))
                {
                    obj.Owner = reader.ReadUInt32(); 
                }
                break;
        }
    }

    private bool IsOwnedObject(uint objClass)
    {
        return objClass is 53 or 57 or 219 or 220;
    }

    private void ReadMonster(H3MReader reader, MapObject obj, MapFormat format)
    {
        if ((uint)format >= 21) obj.Identifier = reader.ReadUInt32(); 
        obj.Quantity = reader.ReadUInt16(); 
        reader.ReadByte(); 
        if (reader.ReadBool()) 
        {
            obj.Message = reader.ReadString(); 
            obj.Resources = new uint[7];
            for(int i=0; i<7; i++) obj.Resources[i] = reader.ReadUInt32(); 
            if ((uint)format >= 21) reader.ReadUInt16(); else reader.ReadByte(); 
        }
        reader.ReadBool(); 
        reader.ReadBool(); 
        reader.Skip(2);
        if ((uint)format >= 32) 
        {
            reader.Skip(4); 
            reader.ReadBool(); 
            reader.ReadUInt32(); 
            reader.ReadUInt32(); 
            reader.ReadUInt32(); 
        }
    }

    private void ReadArtifact(H3MReader reader, MapObject obj, MapFormat format)
    {
        ReadMessageAndGuards(reader, obj, format);
        if (obj.Template!.ObjectClass == 93) 
        {
            obj.Identifier = reader.ReadUInt32(); 
        }
        if ((uint)format >= 32) reader.Skip(5); 
    }

    private void ReadResource(H3MReader reader, MapObject obj, MapFormat format)
    {
        ReadMessageAndGuards(reader, obj, format);
        obj.Quantity = reader.ReadUInt32(); 
        reader.Skip(4);
    }

    private void ReadPandoraOrEvent(H3MReader reader, MapObject obj, MapFormat format)
    {
        ReadMessageAndGuards(reader, obj, format);
        obj.Experience = reader.ReadUInt32();
        obj.Mana = reader.ReadUInt32();
        obj.Morale = (sbyte)reader.ReadByte();
        obj.Luck = (sbyte)reader.ReadByte();
        obj.Resources = new uint[7];
        for(int i=0; i<7; i++) obj.Resources[i] = reader.ReadUInt32(); 
        obj.PrimarySkills = new byte[4];
        for(int i=0; i<4; i++) obj.PrimarySkills[i] = reader.ReadByte(); 
        
        byte skillsCount = reader.ReadByte();
        for(int i=0; i<skillsCount; i++) { reader.ReadByte(); reader.ReadByte(); }
        
        byte artsCount = reader.ReadByte();
        for(int i=0; i<artsCount; i++) 
        {
             if ((uint)format >= 21) reader.ReadUInt16(); else reader.ReadByte();
        }
        
        byte spellsCount = reader.ReadByte();
        for(int i=0; i<spellsCount; i++) reader.ReadByte();
        
        byte creCount = reader.ReadByte();
        if (creCount > 0)
        {
            obj.Guards ??= new List<CreatureStack>();
            for(int i=0; i<creCount; i++) 
            { 
                uint id = (uint)format >= 21 ? reader.ReadUInt16() : reader.ReadByte();
                uint count = reader.ReadUInt16();
                obj.Guards.Add(new CreatureStack { Id = id, Count = count });
            }
        }
        reader.Skip(8);
        if (obj.Template!.ObjectClass == 21) 
        {
            reader.ReadByte(); 
            reader.ReadBool(); 
            reader.ReadBool(); 
            reader.Skip(4);
        }
    }

    private void ReadMessageAndGuards(H3MReader reader, MapObject obj, MapFormat format)
    {
        if (reader.ReadBool())
        {
            obj.Message = reader.ReadString(); 
            obj.Guards = ReadCreatureSet(reader, format);
            reader.Skip(4);
        }
    }

    private List<CreatureStack> ReadCreatureSet(H3MReader reader, MapFormat format)
    {
        var stacks = new List<CreatureStack>();
        for (int i = 0; i < 7; i++)
        {
            uint id = (uint)format >= 21 ? reader.ReadUInt16() : reader.ReadByte();
            uint count = reader.ReadUInt16();
            if (count > 0) stacks.Add(new CreatureStack { Id = id, Count = count });
        }
        return stacks;
    }

    private void ReadTown(H3MReader reader, MapObject obj, MapFormat format) 
    { 
        if ((uint)format >= 21) obj.Identifier = reader.ReadUInt32(); 
        obj.Owner = reader.ReadUInt32(); 
        if (reader.ReadBool()) reader.ReadString(); 
        if (reader.ReadBool()) obj.Guards = ReadCreatureSet(reader, format); 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.Skip(12); else reader.Skip(1); 
        if ((uint)format >= 21) reader.Skip(18); 
        uint eventCount = reader.ReadUInt32(); 
        for(int k=0; k<(int)eventCount; k++) 
        { 
            reader.ReadString(); reader.ReadString();
            reader.Skip(28); reader.ReadByte();
            if ((uint)format >= 28) reader.ReadByte();
            reader.ReadByte(); reader.ReadUInt16(); reader.ReadByte();
            reader.Skip(17); reader.Skip(6);
            reader.Skip((uint)format >= 21 ? 28 : 14); reader.Skip(4);
        }
        if ((uint)format >= 28) reader.ReadByte();
        reader.Skip(3);
    }

    private void ReadHero(H3MReader reader, MapObject obj, MapFormat format) 
    { 
        if ((uint)format >= 21) obj.Identifier = reader.ReadUInt32(); 
        obj.Owner = reader.ReadUInt32(); 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.ReadString(); 
        if (reader.ReadBool()) obj.Experience = reader.ReadUInt32(); 
        if (reader.ReadBool()) reader.ReadByte(); 
        if (reader.ReadBool()) 
        { 
            uint count = reader.ReadUInt32(); 
            reader.Skip((int)count * 2); 
        } 
        if (reader.ReadBool()) obj.Guards = ReadCreatureSet(reader, format); 
        reader.ReadByte(); 
        if (reader.ReadBool()) 
        { 
            if ((uint)format <= 14) reader.Skip(31); 
            else { reader.Skip(40); uint bpCount = reader.ReadUInt16(); reader.Skip((int)bpCount * 4); } 
        } 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.ReadString(); 
        reader.ReadByte(); 
        if (reader.ReadBool()) reader.Skip((uint)format >= 28 ? 9 : 1); 
        if (reader.ReadBool()) reader.Skip(4); 
        reader.Skip(16); 
    }

    private void ReadObjects(H3MReader reader, Map map)
    {
        uint count = 0;
        try { count = reader.ReadUInt32(); } catch { return; }
        for (int i = 0; i < (int)count; i++)
        {
            if (reader.Position + 7 > reader.Length) break;
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
                else { if (!Resync(reader, map)) break; }
            }
            catch { if (!Resync(reader, map)) break; }
        }
    }

    private bool Resync(H3MReader reader, Map map, int maxAttempts = 100)
    {
        long limit = Math.Min(reader.Length, reader.Position + 5000);
        int mapSize = map.BasicInfo.MapSize > 0 ? (int)map.BasicInfo.MapSize : 255;
        int maxIndex = map.ObjectAttributes.Count;
        int attempts = 0;
        while (reader.Position < limit - 7 && attempts++ < maxAttempts)
        {
            long current = reader.Position;
            try 
            {
                byte x = reader.ReadByte(); byte y = reader.ReadByte(); byte z = reader.ReadByte(); 
                uint idx = reader.ReadUInt32();
                if (x < mapSize && y < mapSize && z <= 1 && idx < (uint)maxIndex) 
                { 
                    var template = map.ObjectAttributes[(int)idx];
                    if (!string.IsNullOrEmpty(template.Def) && template.Def.Length > 3)
                    {
                        reader.Position = current; return true; 
                    }
                }
            } catch { }
            reader.Position = current + 1;
        }
        return false;
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
                    TerrainType = reader.ReadByte(), TerrainSprite = reader.ReadByte(),
                    RiverType = reader.ReadByte(), RiverSprite = reader.ReadByte(),
                    RoadType = reader.ReadByte(), RoadSprite = reader.ReadByte(),
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
        
        uint hCount = (uint)map.Format >= 21 ? 156u : 128u;
        if ((uint)map.Format >= 32) 
        { 
            hCount = reader.ReadUInt32();
            if (hCount < 1000) reader.Skip((int)((hCount + 7) / 8));
        }
        else { reader.Skip((uint)map.Format >= 21 ? 20 : 16); }
        
        if ((uint)map.Format >= 21) reader.Skip(18); 
        if ((uint)map.Format >= 28) { reader.Skip(9); reader.Skip(4); }
        if ((uint)map.Format >= 28) ReadCustomHeroes(reader);
        reader.Skip(31);
        if ((uint)map.Format > 14) reader.Skip((uint)map.Format >= 28 ? 18 : 17);
        if ((uint)map.Format >= 28) { reader.Skip(9); reader.Skip(4); }
        ReadRumors(reader); 
        if ((uint)map.Format >= 28 && hCount < 1000) 
        {
            try { ReadHeroSettings(reader, (int)hCount); } catch { }
        }
    }

    private void ReadCustomHeroes(H3MReader reader) 
    { 
        uint count = reader.ReadByte(); 
        for (int i = 0; i < count; i++) 
        { 
            reader.ReadByte(); reader.ReadByte(); reader.ReadString(); reader.ReadByte();
        } 
    }

    private void ReadRumors(H3MReader reader) 
    { 
        try { 
            uint count = reader.ReadUInt32(); 
            if (count < 100) { for (int i = 0; i < count; i++) { reader.ReadString(); reader.ReadString(); } }
        } catch { } 
    }

    private void ReadHeroSettings(H3MReader reader, int count) 
    { 
        for (int i = 0; i < count; i++) 
        { 
            if (!reader.ReadBool()) continue; 
            bool hasExp = reader.ReadBool(); bool hasSkills = reader.ReadBool(); 
            bool hasArtifacts = reader.ReadBool(); bool hasBio = reader.ReadBool(); 
            byte gender = reader.ReadByte(); bool hasSpells = reader.ReadBool(); 
            bool hasPrimary = reader.ReadBool(); 
            
            if (hasExp) reader.ReadUInt32(); 
            if (hasSkills) { uint sc = reader.ReadUInt32(); reader.Skip((int)sc * 2); } 
            if (hasArtifacts) { 
                reader.Skip(18 * 2); uint bpCount = reader.ReadUInt16(); reader.Skip((int)bpCount * 2); 
            } 
            if (hasBio) reader.ReadString(); 
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
            vc.AllowNormalVictory = reader.ReadBool(); vc.AppliesToAI = reader.ReadBool(); 
            switch (vc.Type) { 
                case VictoryConditionType.AcquireArtifact: 
                    vc.ObjectId = (uint)format >= 21 ? reader.ReadUInt16() : reader.ReadByte(); break; 
                case VictoryConditionType.AccumulateCreatures: 
                    vc.ObjectId = (uint)format >= 21 ? reader.ReadUInt16() : reader.ReadByte(); vc.Quantity = reader.ReadUInt32(); break; 
                case VictoryConditionType.AccumulateResources: vc.SecondaryValue = reader.ReadByte(); vc.Quantity = reader.ReadUInt32(); break; 
                case VictoryConditionType.UpgradeTown: case VictoryConditionType.BuildGrail: case VictoryConditionType.DefeatHero: case VictoryConditionType.CaptureTown: case VictoryConditionType.DefeatMonster: vc.Position = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; break; 
                case VictoryConditionType.TransportArtifact: vc.ObjectId = reader.ReadByte(); vc.Position = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; break; 
            } 
        } 
        return vc; 
    }

    private LossCondition ReadLossCondition(H3MReader reader) 
    { 
        var lc = new LossCondition(); 
        lc.Type = (LossConditionType)reader.ReadByte(); 
        if (lc.Type != LossConditionType.None) { 
            switch (lc.Type) { 
                case LossConditionType.LoseTown: case LossConditionType.LoseHero: lc.Position = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; break; 
                case LossConditionType.TimeExpires: lc.Days = reader.ReadUInt16(); break; 
            } 
        } 
        return lc; 
    }

    private void ReadPlayers(H3MReader reader, Map map) 
    { 
        for (int i = 0; i < 8; i++) 
        { 
            var p = new Player { Id = i }; 
            try {
                p.CanBeHuman = reader.ReadBool(); p.CanBeComputer = reader.ReadBool(); 
                if (!p.CanBeHuman && !p.CanBeComputer) {
                    if ((uint)map.Format >= 21) reader.Skip((uint)map.Format >= 28 ? 1 : 6);
                    map.Players.Add(p); continue;
                }
                p.Behavior = reader.ReadByte(); 
                if ((uint)map.Format >= 28) p.AllowedAlignments = reader.ReadByte(); 
                p.AllowedTownsBitmask = reader.ReadByte(); 
                reader.ReadBool(); 
                p.HasMainTown = reader.ReadBool(); 
                if (p.HasMainTown) { 
                    if ((uint)map.Format >= 21) { p.MainTownGenerateHero = reader.ReadBool(); reader.ReadByte(); } 
                    p.MainTownPosition = new Position { X = reader.ReadByte(), Y = reader.ReadByte(), Z = reader.ReadByte() }; 
                } 
                p.MainHeroIsRandom = reader.ReadBool(); p.MainHeroType = reader.ReadByte(); 
                if (p.MainHeroType != 0xFF) { p.MainHeroFace = reader.ReadByte(); p.MainHeroName = reader.ReadString(); } 
                if ((uint)map.Format >= 21) {
                    uint heroCount = reader.ReadUInt32();
                    if (heroCount < 100) {
                        for (uint j = 0; j < heroCount; j++) { reader.ReadByte(); reader.ReadString(); }
                    }
                }
            } catch { }
            map.Players.Add(p); 
        } 
    }
}