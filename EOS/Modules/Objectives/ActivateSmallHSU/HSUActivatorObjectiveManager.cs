using AmorLib.Utils;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EOS.Modules.Objectives.ActivateSmallHSU
{
    internal sealed class HSUActivatorObjectiveManager : InstanceDefinitionManager<HSUActivatorDefinition>
    {
        protected override string DEFINITION_NAME { get; } = "ActivateSmallHSU";
        
        public static HSUActivatorObjectiveManager Current { get; private set; } = new();   
        
        private readonly Dictionary<IntPtr, HSUActivatorDefinition> _hsuActivatorPuzzles = new(); // key: ChainedPuzzleInstance.Pointer    

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnBuildDone()
        {
            if (Definitions.TryGetValue(RundownManager.ActiveExpedition.LevelLayoutData, out var defs))
            {
                defs.Definitions.ForEach(BuildHSUActivatorChainedPuzzle);
            }
        }

        protected override void OnLevelCleanup()
        {
            foreach (var h in _hsuActivatorPuzzles.Values)
            {
                h.ChainedPuzzleOnActivationInstance = null!;
            }

            _hsuActivatorPuzzles.Clear();
        }

        protected override void AddDefinitions(InstanceDefinitionsForLevel<HSUActivatorDefinition> definitions)
        {
            // because we have chained puzzles, sorting is necessary to preserve chained puzzle instance order.
            Sort(definitions);
            base.AddDefinitions(definitions);
        }

        private void BuildHSUActivatorChainedPuzzle(HSUActivatorDefinition def)
        {
            if (!HSUActivatorInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var instance))
            {
                EOSLogger.Error($"Found unused HSUActivator config: {(def.DimensionIndex, def.LayerType, def.LocalIndex, def.InstanceIndex)}");
                return;
            }

            if (def.RequireItemAfterActivationInExitScan)
            {
                instance.m_sequencerExtractionDone.OnSequenceDone += new Action(() => 
                {
                    WardenObjectiveManager.AddObjectiveItemAsRequiredForExitScan(true, new iWardenObjectiveItem[1] { new(instance.m_linkedItemComingOut.Pointer) });
                    EOSLogger.Debug($"HSUActivator: {def} - added required item for extraction scan");
                });
            }

            if (def.TakeOutItemAfterActivation)
            {
                instance.m_sequencerExtractionDone.OnSequenceDone += new Action(() => 
                {
                    instance.LinkedItemComingOut.m_navMarkerPlacer.SetMarkerVisible(true);
                });
            }

            if (def.ChainedPuzzleOnActivation != 0)
            {
                if (!DataBlockHelper.TryGetBlock<ChainedPuzzleDataBlock>(def.ChainedPuzzleOnActivation, out var block))
                {
                    EOSLogger.Error($"HSUActivator: ChainedPuzzleOnActivation is specified but ChainedPuzzleDatablock definition is not found, won't build");
                    return;
                }
                
                Vector3 startPosition = def.ChainedPuzzleStartPosition.ToVector3();
                if (startPosition == Vector3.zeroVector)
                {
                    startPosition = instance.m_itemGoingInAlign.position;
                }

                var puzzleInstance = ChainedPuzzleManager.CreatePuzzleInstance
                (
                    block,
                    instance.SpawnNode.m_area,
                    startPosition,
                    instance.SpawnNode.m_area.transform
                );
                def.ChainedPuzzleOnActivationInstance = puzzleInstance;
                _hsuActivatorPuzzles[puzzleInstance.Pointer] = def;

                // PuzzleInstance will be activated in SyncStateChanged
                // EventsOnActivationScanSolved and HSU removeSequence will be executed in ChainedPuzzleInstance.OnStateChanged(patch ChainedPuzzleInstance_OnPuzzleSolved)
                puzzleInstance.Add_OnStateChange((_, newState, isRecall) =>
                {
                    if (newState.status == eChainedPuzzleStatus.Solved && !isRecall)
                    {
                        EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnActivationScanSolved);

                        if (def.TakeOutItemAfterActivation)
                            instance.m_triggerExtractSequenceRoutine = instance.StartCoroutine(instance.TriggerRemoveSequence());
                    }
                });

                EOSLogger.Debug($"HSUActivator: ChainedPuzzleOnActivation ID: {def.ChainedPuzzleOnActivation} specified and created");
            }
            else if (def.TakeOutItemAfterActivation)
            {
                instance.m_triggerExtractSequenceRoutine = instance.StartCoroutine(instance.TriggerRemoveSequence());
            }
        }

        internal HSUActivatorDefinition GetHSUActivatorDefinition(ChainedPuzzleInstance chainedPuzzle) => _hsuActivatorPuzzles.TryGetValue(chainedPuzzle.Pointer, out var def) ? def : null!;

        public bool TryGetDefinition(LG_HSUActivator_Core instance, [MaybeNullWhen(false)] out HSUActivatorDefinition definition)
        {
            var (globalIndex, instanceIndex) = HSUActivatorInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }
    }
}