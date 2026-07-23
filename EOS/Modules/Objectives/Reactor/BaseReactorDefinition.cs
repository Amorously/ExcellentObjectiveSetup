using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.BaseClasses.CustomTerminalDefinition;
using EOS.Modules.Tweaks.TerminalTweak;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.Reactor
{
    public class BaseReactorDefinition : BaseInstanceDefinition
    {
        [JsonPropertyOrder(-7)]
        public TerminalDefinition ReactorTerminal { set; get; } = new();

        [JsonPropertyOrder(-7)]
        public List<WardenObjectiveEventData> EventsOnActive { get; set; } = new();

        public SerialGeneratorManager.CodeWordLength CodeWordLength { get; set; } = SerialGeneratorManager.CodeWordLength.Four;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleToActiveInstance { get; set; } = null!;
    }
}
