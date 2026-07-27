using GameData;
using LevelGeneration;

namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class BaseTerminalDefinition // same as BaseDefinition, but redeclare one for readability
    {
        public eDimensionIndex DimensionIndex { get; set; }

        public LG_LayerType Layer { get; set; }

        public LG_LayerType LayerType { private get => Layer; set => Layer = value; }

        public eLocalZoneIndex LocalIndex { get; set; }

        public uint InstanceIndex { get; set; } = uint.MaxValue;

        public (int, int, int) GlobalIndexTuple() => ((int)DimensionIndex, (int)Layer, (int)LocalIndex);
    }

    public class TerminalDefinition
    {
        public List<TerminalLogFileData> LocalLogFiles { get; set; } = new();

        public List<CustomCommand> UniqueCommands { get; set; } = new() { new() };

        public TerminalPasswordData PasswordData { get; set; } = new();
    }
}
