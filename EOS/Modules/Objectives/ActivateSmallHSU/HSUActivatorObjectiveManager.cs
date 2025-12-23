using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EOS.Modules.Objectives.ActivateSmallHSU
{
    internal sealed class HSUActivatorObjectiveManager : InstanceDefinitionManager<HSUActivatorDefinition, HSUActivatorObjectiveManager>
    {
        protected override string DEFINITION_NAME { get; } = "ActivateSmallHSU";
        public override uint ChainedPuzzleLoadOrder => 2u;
        
        private readonly Dictionary<IntPtr, HSUActivatorDefinition> _hsuActivatorPuzzles = new(); // key: ChainedPuzzleInstance.Pointer    

        protected override void OnBuildDone()
        {
            if (InstanceDefinitions.TryGetValue(CurrentMainLevelLayout, out var defs))
            {
                defs.Definitions.ForEach(BuildHSUActivatorChainedPuzzle);
            }
        }
        
        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _hsuActivatorPuzzles.ForEachValue(h => h.ChainedPuzzleOnActivationInstance = null!);
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
                EOSLogger.Error($"Found unused HSUActivator config: {def}");
                return;
            }

            bool extractAnimDone = false;
            instance.m_sequencerExtractionDone.OnSequenceDone += new Action(() =>
            {
                extractAnimDone = true;

                if (def.RequireItemAfterActivationInExitScan)
                {
                    WardenObjectiveManager.AddObjectiveItemAsRequiredForExitScan(true, new iWardenObjectiveItem[1] { new(instance.m_linkedItemComingOut.Pointer) });
                    EOSLogger.Debug($"HSUActivator: {def} - added required item for extraction scan");
                }

                if (def.TakeOutItemAfterActivation)
                {
                    instance.LinkedItemComingOut.m_navMarkerPlacer.SetMarkerVisible(true);
                }
            });            

            if (def.ChainedPuzzleOnActivation == 0u)
            {
                TriggerRemoveSequence(def.TakeOutItemAfterActivation);
                return;
            }

            if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(def.ChainedPuzzleOnActivation, out var block))
            {
                EOSLogger.Error($"HSUActivator: ChainedPuzzleOnActivation is specified but ChainedPuzzleDatablock definition is not found, won't build");
                return;
            }

            Vector3 startPosition = def.ChainedPuzzleStartPosition == Vector3.zeroVector ? instance.m_itemGoingInAlign.position : def.ChainedPuzzleStartPosition;
            var puzzleInstance = ChainedPuzzleManager.CreatePuzzleInstance(block, instance.SpawnNode.m_area, startPosition, instance.SpawnNode.m_area.transform);
            def.ChainedPuzzleOnActivationInstance = puzzleInstance;
            _hsuActivatorPuzzles[puzzleInstance.Pointer] = def;

            // PuzzleInstance will be activated in SyncStateChanged
            // EventsOnActivationScanSolved and HSU removeSequence will be executed in ChainedPuzzleInstance.OnStateChanged(patch ChainedPuzzleInstance_OnPuzzleSolved)
            puzzleInstance.Add_OnStateChange((_, newState, isRecall) =>
            {
                if (newState.status != eChainedPuzzleStatus.Solved || isRecall) 
                    return;
                EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnActivationScanSolved);
                TriggerRemoveSequence(def.TakeOutItemAfterActivation);
            });

            EOSLogger.Debug($"HSUActivator: ChainedPuzzleOnActivation ID: {def.ChainedPuzzleOnActivation} specified and created");

            void TriggerRemoveSequence(bool takeOutItemAfterActivation)
            {
                instance.m_sequencerExtractionDone.OnSequenceDone += new Action(() =>
                {
                    if (takeOutItemAfterActivation && extractAnimDone)
                    {
                        instance.m_triggerExtractSequenceRoutine = instance.StartCoroutine(instance.TriggerRemoveSequence());
                    }
                });
            }
        }

        internal HSUActivatorDefinition? GetHSUActivatorDefinition(ChainedPuzzleInstance chainedPuzzle) => _hsuActivatorPuzzles.TryGetValue(chainedPuzzle.Pointer, out var def) ? def : null;

        public bool TryGetDefinition(LG_HSUActivator_Core instance, [MaybeNullWhen(false)] out HSUActivatorDefinition definition)
        {
            var (globalIndex, instanceIndex) = HSUActivatorInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }
    }
}