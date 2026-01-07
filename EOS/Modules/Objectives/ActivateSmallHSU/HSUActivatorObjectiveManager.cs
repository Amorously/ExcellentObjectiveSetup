using AmorLib.Events;
using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using BepInEx.Unity.IL2CPP.Utils;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using LevelGeneration;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EOS.Modules.Objectives.ActivateSmallHSU
{
    internal sealed class HSUActivatorObjectiveManager : InstanceDefinitionManager<HSUActivatorDefinition, HSUActivatorObjectiveManager>
    {
        protected override string DEFINITION_NAME { get; } = "ActivateSmallHSU";
        public override uint ChainedPuzzleLoadOrder => 2u;
        
        private readonly Dictionary<IntPtr, HSUActivatorDefinition> _hsuActivatorPuzzles = new(); // key: ChainedPuzzleInstance.Pointer    

        public HSUActivatorObjectiveManager()
        {
            SNetEvents.OnRecallDone += OnEnterLevel; // seems SyncStatusChanged patch is not called on recall?
        }

        protected override void AddDefinitions(InstanceDefinitionsForLevel<HSUActivatorDefinition> definitions)
        {
            // because we have chained puzzles, sorting is necessary to preserve chained puzzle instance order.
            Sort(definitions);
            base.AddDefinitions(definitions);
        }

        internal HSUActivatorDefinition? GetHSUActivatorDefinition(ChainedPuzzleInstance chainedPuzzle) => _hsuActivatorPuzzles.TryGetValue(chainedPuzzle.Pointer, out var def) ? def : null;

        public bool TryGetDefinition(LG_HSUActivator_Core instance, [MaybeNullWhen(false)] out HSUActivatorDefinition definition)
        {
            var (globalIndex, instanceIndex) = HSUActivatorInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }

        protected override void OnBuildDone()
        {
            foreach (var def in GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                BuildHSUActivatorChainedPuzzle(def);
            }
        }

        protected override void OnEnterLevel()
        {
            foreach (var def in GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                if (!HSUActivatorInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var instance))
                    return;

                instance.StartCoroutine(DelayedCullingSetup(instance));
            }
        }

        private static IEnumerator DelayedCullingSetup(LG_HSUActivator_Core instance)
        {
            yield return new WaitForSeconds(1.5f); // wait for recall to finish
            instance.PostCullingSetup();
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _hsuActivatorPuzzles.ForEachValue(h => h.ChainedPuzzleOnActivationInstance = null!);
            _hsuActivatorPuzzles.Clear();
        }

        private void BuildHSUActivatorChainedPuzzle(HSUActivatorDefinition def)
        {
            if (!HSUActivatorInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var instance))
            {
                EOSLogger.Error($"Found unused HSUActivator config: {def}");
                return;
            }

            bool insertAnimDone = false;
            instance.m_sequencerWaitingForItem.OnSequenceDone += new Action(() => insertAnimDone = false);
            instance.m_sequencerInsertItem.OnSequenceDone += new Action(() => insertAnimDone = true);

            instance.m_sequencerExtractionDone.OnSequenceDone += new Action(() =>
            {
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
                if (def.TakeOutItemAfterActivation)
                {
                    instance.m_triggerExtractSequenceRoutine = instance.StartCoroutine(instance.TriggerRemoveSequence());
                }
                return;
            }

            if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(def.ChainedPuzzleOnActivation, out var block))
            {
                EOSLogger.Error("HSUActivator: ChainedPuzzleOnActivation is specified but ChainedPuzzleDatablock definition is not found, won't build");
                return;
            }

            Vector3 startPosition = def.ChainedPuzzleStartPosition == Vector3.zeroVector ? instance.m_itemGoingInAlign.position : def.ChainedPuzzleStartPosition;
            var puzzleInstance = ChainedPuzzleManager.CreatePuzzleInstance(block, instance.SpawnNode.m_area, startPosition, instance.SpawnNode.m_area.transform);
            def.ChainedPuzzleOnActivationInstance = puzzleInstance;
            _hsuActivatorPuzzles[puzzleInstance.Pointer] = def;

            puzzleInstance.Add_OnStateChange((_, newState, isRecall) =>
            {
                if (newState.status != eChainedPuzzleStatus.Solved || isRecall) 
                    return;

                EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnActivationScanSolved);

                if (!def.TakeOutItemAfterActivation)
                    return;

                if (!insertAnimDone)
                {
                    instance.m_sequencerInsertItem.OnSequenceDone += new Action(() =>
                    {
                        instance.m_triggerExtractSequenceRoutine = instance.StartCoroutine(instance.TriggerRemoveSequence());
                    });
                }
                else
                {
                    instance.m_triggerExtractSequenceRoutine = instance.StartCoroutine(instance.TriggerRemoveSequence());
                }
            });

            EOSLogger.Debug($"HSUActivator: ChainedPuzzleOnActivation ID: {def.ChainedPuzzleOnActivation} specified and created");
        }
    }
}
