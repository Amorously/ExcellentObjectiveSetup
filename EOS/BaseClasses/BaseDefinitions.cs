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

    public class BaseInstanceDefinition : GlobalBased
    {
        [JsonPropertyOrder(-8)]
        public uint InstanceIndex { get; set; } = uint.MaxValue;

        public override string ToString() => base.ToString() + $", Instance_{InstanceIndex}";
    }
}
