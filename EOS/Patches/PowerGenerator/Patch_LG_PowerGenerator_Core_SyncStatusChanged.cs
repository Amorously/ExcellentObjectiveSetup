using BepInEx.Unity.IL2CPP.Utils.Collections;
using EOS.Modules.Expedition.IndividualGeneratorGroup;
using EOS.Modules.Instances;
using EOS.Modules.Objectives.GeneratorCluster;
using EOS.Modules.Objectives.IndividualGenerator;
using GameData;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.PowerGenerator
{
    [HarmonyPatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.SyncStatusChanged))]
    internal static class Patch_LG_PowerGenerator_Core_SyncStatusChanged
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_SyncStatusChanged(LG_PowerGenerator_Core __instance, pPowerGeneratorState state, bool isDropinState)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                return;

            var status = state.status;

            // ==================== ==================== ====================
            // ====================   generator cluster  ====================
            // ==================== ==================== ====================            
            var gcParent = PowerGeneratorInstanceManager.Current.GetParentGeneratorCluster(__instance);
            if(gcParent != null && GeneratorClusterObjectiveManager.Current.TryGetDefinition(gcParent, out var gcDef))
            {
                EOSLogger.Log($"LG_PowerGeneratorCluster.powerGenerator.OnSyncStatusChanged! status: {status}, isDropinState: {isDropinState}");

                if (status == ePowerGeneratorStatus.Powered)
                {
                    uint poweredGenerators = 0u;
                    for (int m = 0; m < gcParent.m_generators.Length; ++m)
                    {
                        if (gcParent.m_generators[m].m_stateReplicator.State.status == ePowerGeneratorStatus.Powered)
                            poweredGenerators++;
                    }

                    EOSLogger.Log($"Generator Cluster PowerCell inserted ({poweredGenerators} / {gcParent.m_generators.Count})");
                    var EventsOnInsertCell = gcDef.EventsOnInsertCell;

                    int eventsIndex = (int)(poweredGenerators - 1);

                    if(!isDropinState)
                    {
                        if (eventsIndex >= 0 && eventsIndex < EventsOnInsertCell.Count)
                        {
                            EOSLogger.Log($"Executing events ({poweredGenerators} / {gcParent.m_generators.Count}). Event count: {EventsOnInsertCell[eventsIndex].Count}");
                            EventsOnInsertCell[eventsIndex].ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
                        }

                        if (poweredGenerators == gcParent.m_generators.Count && !gcParent.m_endSequenceTriggered)
                        {
                            EOSLogger.Log("All generators powered, executing end sequence");
                            __instance.StartCoroutine(gcParent.ObjectiveEndSequence());
                            gcParent.m_endSequenceTriggered = true;
                        }
                    }
                    else
                    {
                        if(poweredGenerators != gcParent.m_generators.Count)
                        {
                            gcParent.m_endSequenceTriggered = false;
                        }
                    }
                } 
                else if (isDropinState) // Reloading from checkpoint and generator is unpowered, thus cluster is implicitly unfinished
                {
                    gcParent.m_endSequenceTriggered = false;
                }
            }

            // ==================== ==================== ====================
            // ==================== individual generator ====================
            // ==================== ==================== ====================
            if(IndividualGeneratorObjectiveManager.Current.TryGetDefinition(__instance, out var igDef) && igDef.EventsOnInsertCell != null && status == ePowerGeneratorStatus.Powered && !isDropinState)
            {
                EOSWardenEventManager.ExecuteWardenEvents(igDef.EventsOnInsertCell);
            }

            // ==================== ==================== ====================
            // =============== individual generator group ===================
            // ==================== ==================== ====================
            var igGroupDef = ExpeditionIGGroupManager.Current.FindGroupDefOf(__instance);
            if (igGroupDef != null)
            {
                int poweredGeneratorCount = 0;
                foreach (var g in igGroupDef.GeneratorInstances)
                {
                    if (g.m_stateReplicator.State.status == ePowerGeneratorStatus.Powered)
                    {
                        poweredGeneratorCount += 1;
                    }
                }

                if(!isDropinState)
                {
                    if (poweredGeneratorCount == igGroupDef.GeneratorInstances.Count && igGroupDef.PlayEndSequenceOnGroupComplete)
                    {
                        var coroutine = CoroutineManager.StartCoroutine(ExpeditionIGGroupManager.PlayGroupEndSequence(igGroupDef).WrapToIl2Cpp());
                        WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
                    }

                    else
                    {
                        int eventIndex = poweredGeneratorCount - 1;
                        if (eventIndex >= 0 && eventIndex < igGroupDef.EventsOnInsertCell.Count)
                        {
                            EOSWardenEventManager.ExecuteWardenEvents(igGroupDef.EventsOnInsertCell[eventIndex]);
                        }
                    }
                }
            }
        }
    }
}
