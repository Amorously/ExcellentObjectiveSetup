using AmorLib.Utils;
using GameData;
using LevelGeneration;
using System.Text.Json.Serialization;

namespace EOS.BaseClasses
{
    public class GlobalBased : GlobalBase
    {
        [JsonPropertyOrder(-10)]
        public LG_LayerType LayerType { get => Layer; private set => Layer = value; } // name consistency

        public (eDimensionIndex, LG_LayerType, eLocalZoneIndex) GlobalZoneIndexTuple() => (DimensionIndex, LayerType, LocalIndex);
    }

    public class ZoneDefinitionsForLevel<T> where T : GlobalBased, new()
    {
        public uint MainLevelLayout { set; get; } = 0;
        public List<T> Definitions { set; get; } = new() { new() };
    }
}
