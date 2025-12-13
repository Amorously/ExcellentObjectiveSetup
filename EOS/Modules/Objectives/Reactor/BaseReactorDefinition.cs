using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.BaseClasses.CustomTerminalDefinition;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.Reactor
{
    public class BaseReactorDefinition : BaseInstanceDefinition
    {
        [JsonPropertyOrder(-9)]
        public TerminalDefinition ReactorTerminal { set; get; } = new();

        [JsonPropertyOrder(-9)]
        public List<WardenObjectiveEventData> EventsOnActive { get; set; } = new();

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleToActiveInstance { get; set; } = null!;
    }
}
