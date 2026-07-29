using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.BaseClasses.CustomTerminalDefinition;
using EOS.Modules.Tweaks.TerminalTweak;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.Reactor
{
    public class BaseReactorDefinition : BaseInstanceDefinition // idk why but the properties are serialized in the wrong order
    {
        [JsonPropertyOrder(-8)]
        public TerminalDefinition ReactorTerminal { get; set; } = new();

        [JsonPropertyOrder(-7)]
        public List<WardenObjectiveEventData> EventsOnActive { get; set; } = new();

        [JsonPropertyOrder(-1)]
        public SerialGeneratorManager.CodeWordLength CodeWordLength { get; set; } = SerialGeneratorManager.CodeWordLength.Four;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleToActiveInstance { get; set; } = null!;
    }
}
