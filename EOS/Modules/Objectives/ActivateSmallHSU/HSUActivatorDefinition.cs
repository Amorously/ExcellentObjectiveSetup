using ChainedPuzzles;
using EOS.BaseClasses;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.ActivateSmallHSU
{
    public class HSUActivatorDefinition: BaseInstanceDefinition
    { 
        public List<WardenObjectiveEventData> EventsOnHSUActivation { get; set; } = new();

        public uint ItemFromStart { get; set; } = 0u;

        public uint ItemAfterActivation { get; set; } = 0u;

        public bool RequireItemAfterActivationInExitScan { get; set; } = false;

        public bool TakeOutItemAfterActivation { get; set; } = true;

        public uint ChainedPuzzleOnActivation { get; set; } = 0u;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleOnActivationInstance { get; set; } = null!;

        public Vec3 ChainedPuzzleStartPosition { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnActivationScanSolved { get; set; } = new();
    }
}
