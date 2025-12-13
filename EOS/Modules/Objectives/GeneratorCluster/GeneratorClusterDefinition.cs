using EOS.BaseClasses;
using GameData;

namespace EOS.Modules.Objectives.GeneratorCluster
{
    public class GeneratorClusterDefinition : BaseInstanceDefinition // OnActivateOnSolveItem is enabled by default
    {
        public uint NumberOfGenerators { get; set; } = 0;
        public List<List<WardenObjectiveEventData>> EventsOnInsertCell { get; set; } = new() { new() };
        public uint EndSequenceChainedPuzzle { get; set; } = 0u;
        public List<WardenObjectiveEventData> EventsOnEndSequenceChainedPuzzleComplete { get; set; } = new() { };
    }
}
