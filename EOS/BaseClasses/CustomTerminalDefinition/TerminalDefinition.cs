using GameData;
using LevelGeneration;
using System.Text.Json.Serialization;

namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class BaseTerminalDefinition // same as BaseDefinition, but redeclare one for readability
    {
        [JsonPropertyOrder(-10)]
        public eDimensionIndex DimensionIndex { get; set; }

        [JsonPropertyOrder(-10)]
        public LG_LayerType Layer { get; set; }

        [JsonPropertyOrder(-10)]
        public LG_LayerType LayerType { private get => Layer; set => Layer = value; }

        [JsonPropertyOrder(-10)]
        public eLocalZoneIndex LocalIndex { get; set; }

        [JsonPropertyOrder(-9)]
        public uint InstanceIndex { get; set; } = uint.MaxValue;

        public (int, int, int) GetIntTuple() => ((int)DimensionIndex, (int)Layer, (int)LocalIndex);
    }

    public class TerminalDefinition
    {
        public List<TerminalLogFileData> LocalLogFiles { get; set; } = new();

        public List<CustomCommand> UniqueCommands { get; set; } = new() { new() };

        public TerminalPasswordData PasswordData { get; set; } = new();
    }
}
