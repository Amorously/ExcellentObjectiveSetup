using AmorLib.Utils;
using GameData;
using LevelGeneration;
using System.Text.Json.Serialization;

namespace EOS.BaseClasses
{
    public class GlobalBased : GlobalBase
    {
        [JsonPropertyOrder(-10)]
        public LG_LayerType LayerType { private get => Layer; set => Layer = value; } // name consistency, backwards compatibility

        public (eDimensionIndex, LG_LayerType, eLocalZoneIndex) GlobalZoneIndexTuple() => (DimensionIndex, Layer, LocalIndex);
    }

    public class ZoneDefinitionsForLevel<T> where T : GlobalBased, new()
    {
        public uint MainLevelLayout { set; get; } = 0;

        public List<T> Definitions { set; get; } = new() { new() };
    }
}
