using AmorLib.Utils;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Objectives.GeneratorCluster
{
    internal sealed class GeneratorClusterObjectiveManager : InstanceDefinitionManager<GeneratorClusterDefinition>
    {
        protected override string DEFINITION_NAME { get; } = "GeneratorCluster";
        
        public static GeneratorClusterObjectiveManager Current { get; private set; } = new();        

        private List<(LG_PowerGeneratorCluster, GeneratorClusterDefinition)> _chainedPuzzleToBuild = new();

        protected override void OnBuildStart() => OnLevelCleanup(); 

        protected override void OnBuildDone() // BuildChainedPuzzleMidObjective
        {
            foreach (var (instance, config) in _chainedPuzzleToBuild)
            {
                if (DataBlockHelper.TryGetBlock<ChainedPuzzleDataBlock>(config.EndSequenceChainedPuzzle, out var block))
                {
                    EOSLogger.Debug($"GeneratorCluster: Building EndSequenceChainedPuzzle for LG_PowerGeneratorCluster in {instance.SpawnNode.m_zone.ToStruct()}");

                    instance.m_chainedPuzzleMidObjective = ChainedPuzzleManager.CreatePuzzleInstance
                    (
                        block,
                        instance.SpawnNode.m_area,
                        instance.m_chainedPuzzleAlignMidObjective.position,
                        instance.m_chainedPuzzleAlignMidObjective
                    );

                    instance.m_chainedPuzzleMidObjective.Add_OnStateChange((_, newState, isRecall) =>
                    {
                        if (newState.status == eChainedPuzzleStatus.Solved && !isRecall)
                            EOSWardenEventManager.ExecuteWardenEvents(config.EventsOnEndSequenceChainedPuzzleComplete);
                    });
                }
            }
        }

        protected override void OnLevelCleanup()
        {
            _chainedPuzzleToBuild.Clear();
        }
        
        protected override void AddDefinitions(InstanceDefinitionsForLevel<GeneratorClusterDefinition> definitions)
        {
            // because we have chained puzzles, sorting is necessary to preserve chained puzzle instance order.
            Sort(definitions);
            base.AddDefinitions(definitions);
        }

        internal void RegisterForChainedPuzzleBuild(LG_PowerGeneratorCluster instance, GeneratorClusterDefinition GeneratorClusterConfig) => _chainedPuzzleToBuild.Add((instance, GeneratorClusterConfig));

        public bool TryGetDefinition(LG_PowerGeneratorCluster instance, [MaybeNullWhen(false)] out GeneratorClusterDefinition definition)
        {
            var (globalIndex, instanceIndex) = GeneratorClusterInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }
    }
}
