using ChainedPuzzles;
using EOS.BaseClasses;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.Reactor
{
    public class ReactorShutdownDefinition : BaseReactorDefinition
    {
        [JsonPropertyOrder(-7)]
        public uint ChainedPuzzleToActive { get; set; } = 0u;

        [JsonPropertyOrder(-6)]
        public bool PutVerificationCodeOnTerminal { get; set; } = false;

        [JsonPropertyOrder(-5)]
        public BaseInstanceDefinition VerificationCodeTerminal { get; set; } = new();

        [JsonPropertyOrder(0)]
        public uint ChainedPuzzleOnVerification { get; set; } = 0u;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleOnVerificationInstance { get; set; } = null!;

        [JsonPropertyOrder(1)]
        public List<WardenObjectiveEventData> EventsOnShutdownPuzzleStarts { get; set; } = new();

        [JsonPropertyOrder(2)]
        public List<WardenObjectiveEventData> EventsOnComplete { get; set; } = new();
    }
}
