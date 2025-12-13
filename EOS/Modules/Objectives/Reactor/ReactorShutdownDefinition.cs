using ChainedPuzzles;
using EOS.BaseClasses;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.Reactor
{
    public class ReactorShutdownDefinition : BaseReactorDefinition
    {
        [JsonPropertyOrder(-9)]
        public uint ChainedPuzzleToActive { get; set; } = 0u;

        public bool PutVerificationCodeOnTerminal { get; set; } = false;

        public BaseInstanceDefinition VerificationCodeTerminal { get; set; } = new();

        public uint ChainedPuzzleOnVerification { get; set; } = 0u;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleOnVerificationInstance { get; set; } = null!;

        public List<WardenObjectiveEventData> EventsOnShutdownPuzzleStarts { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnComplete { get; set; } = new();
    }
}
