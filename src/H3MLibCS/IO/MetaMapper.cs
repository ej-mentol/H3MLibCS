using H3M.Models;

namespace H3M.IO;

public static class MetaMapper
{
    public static MetaType GetMetaType(uint objectClass)
    {
        return objectClass switch
        {
            0 => MetaType.GenericImpassableTerrain,
            1 => MetaType.GenericVisitable,
            2 => MetaType.GenericVisitable,
            3 => MetaType.GenericVisitable,
            4 => MetaType.GenericVisitable,
            5 => MetaType.Artifact,
            6 => MetaType.PandorasBox,
            7 => MetaType.GenericVisitable,
            8 => MetaType.GenericBoat,
            9 => MetaType.Town, // Cove
            10 => MetaType.Town, // Factory
            11 => MetaType.Town, // Kronverk (if any)
            
            17 => MetaType.Dwelling, // Creature Generator (Dwelling)
            18 => MetaType.Dwelling,
            19 => MetaType.Dwelling,
            20 => MetaType.Dwelling,
            
            26 => MetaType.Event,
            33 => MetaType.Garrison,
            34 => MetaType.Hero,
            36 => MetaType.Grail,
            42 => MetaType.Lighthouse,
            53 => MetaType.ResourceGenerator,
            54 => MetaType.Monster,
            62 => MetaType.Prison,
            70 => MetaType.RandomHero,
            71 => MetaType.Monster,
            76 => MetaType.Resource,
            77 => MetaType.RandomTown,
            78 => MetaType.Resource, // Resource
            79 => MetaType.Resource, // Resource
            81 => MetaType.Scholar,
            83 => MetaType.SeersHut,
            87 => MetaType.Shipyard,
            91 => MetaType.Sign,
            93 => MetaType.SpellScroll,
            98 => MetaType.Town, // Standard Town
            
            // HotA Specific
            100 => MetaType.GenericVisitable, // Cannon Yard
            101 => MetaType.GenericVisitable, // Warehouse
            
            103 => MetaType.SubterraneanGate,
            113 => MetaType.WitchHut,
            214 => MetaType.PlaceholderHero,
            215 => MetaType.QuestGuard,
            216 => MetaType.RandomDwellingAbSoD,
            219 => MetaType.GarrisonAbSoD,
            220 => MetaType.AbandonedMineAbSoD,
            
            // Ranges
            >= 114 and <= 161 => MetaType.GenericImpassableTerrain,
            >= 165 and <= 211 => MetaType.GenericImpassableTerrainAbSoD,
            >= 222 and <= 231 => MetaType.GenericPassableTerrainSoD,
            
            _ => MetaType.Unknown
        };
    }
}
