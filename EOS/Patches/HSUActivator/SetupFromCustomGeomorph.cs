using EOS.Modules.Instances;
using EOS.Modules.Objectives.ActivateSmallHSU;
using HarmonyLib;
using LevelGeneration;
using Player;
using SNetwork;

namespace EOS.Patches.HSUActivator
{
    [HarmonyPatch(typeof(LG_HSUActivator_Core), nameof(LG_HSUActivator_Core.SetupFromCustomGeomorph))]
    internal static class SetupFromCustomGeomorph
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_HSUActivator_Core_SetupFromCustomGeomorph(LG_HSUActivator_Core __instance)
        {
            HSUActivatorInstanceManager.Current.Register(__instance);

            if (!HSUActivatorObjectiveManager.Current.TryGetDefinition(__instance, out var def)) 
                return;

            if (__instance.m_isWardenObjective)
            {
                EOSLogger.Error($"BuildCustomHSUActivator: the HSUActivator has been set up by vanilla! Aborting custom setup...");
                EOSLogger.Error($"HSUActivator in {__instance.SpawnNode.m_zone.LocalIndex}, {__instance.SpawnNode.LayerType}, {__instance.SpawnNode.m_dimension.DimensionIndex}");
                return;
            }

            // LG_HSUActivator_Core.Setup
            __instance.m_linkedItemGoingIn = __instance.SpawnPickupItemOnAlign(def.ItemFromStart, __instance.m_itemGoingInAlign, false, -1);
            __instance.m_linkedItemComingOut = __instance.SpawnPickupItemOnAlign(def.ItemAfterActivation, __instance.m_itemComingOutAlign, false, -1);

            LG_LevelInteractionManager.DeregisterTerminalItem(__instance.m_linkedItemGoingIn.GetComponentInChildren<iTerminalItem>());
            LG_LevelInteractionManager.DeregisterTerminalItem(__instance.m_linkedItemComingOut.GetComponentInChildren<iTerminalItem>());
            __instance.m_linkedItemGoingIn.SetPickupInteractionEnabled(false);
            __instance.m_linkedItemComingOut.SetPickupInteractionEnabled(false);

            __instance.m_insertHSUInteraction.OnInteractionSelected = new Action<PlayerAgent>((p) => { }); // reset, do nothing, do not interfere with warden objective
            __instance.m_sequencerInsertItem.OnSequenceDone = new Action(() =>
            {
                pHSUActivatorState state = __instance.m_stateReplicator.State;
                if (!state.isSequenceIncomplete)
                    EOSLogger.Log(">>>>>> HSUInsertSequenceDone! Sequence was already complete");

                state.isSequenceIncomplete = false;
                __instance.m_stateReplicator.SetStateUnsynced(state);

                EOSLogger.Log(">>>>>> HSUInsertSequenceDone!");                
                if (__instance.m_triggerExtractSequenceRoutine != null)
                    __instance.StopCoroutine(__instance.m_triggerExtractSequenceRoutine);
            });

            __instance.m_sequencerExtractItem.OnSequenceDone = new Action(() =>
            {
                __instance.m_stateReplicator.SetStateUnsynced(__instance.m_stateReplicator.State with { isSequenceIncomplete = true });
                if (SNet.IsMaster)
                {
                    __instance.AttemptInteract(new pHSUActivatorInteraction() { type = eHSUActivatorInteractionType.SetExtractDone });
                }
            });

            EOSLogger.Debug($"HSUActivator: {(def.DimensionIndex, def.LayerType, def.LocalIndex, def.InstanceIndex)}, custom setup complete");
        }
    }
}
