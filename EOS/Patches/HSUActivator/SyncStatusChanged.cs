using EOS.Modules.Instances;
using EOS.Modules.Objectives.ActivateSmallHSU;
using HarmonyLib;
using LevelGeneration;
using SNetwork;

namespace EOS.Patches.HSUActivator
{
    [HarmonyPatch(typeof(LG_HSUActivator_Core), nameof(LG_HSUActivator_Core.SyncStatusChanged))]
    internal static class SyncStatusChanged
    {        
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_HSUActivator_Core_SyncStatusChanged(LG_HSUActivator_Core __instance, pHSUActivatorState newState, bool isRecall)
        {
            if (__instance.m_isWardenObjective) return true;
            
            uint index = HSUActivatorInstanceManager.Current.GetZoneInstanceIndex(__instance);
            if (index == uint.MaxValue)
            {
                EOSLogger.Error($"Found unregistered HSUActivator!! {HSUActivatorInstanceManager.Current.GetGlobalIndex(__instance)}");
                return true;
            }

            if (!HSUActivatorObjectiveManager.Current.TryGetDefinition(__instance, out var def))
                return true;

            if (__instance.m_triggerExtractSequenceRoutine != null)
                __instance.StopCoroutine(__instance.m_triggerExtractSequenceRoutine);

            bool goingInVisibleForPostCulling = __instance.m_goingInVisibleForPostCulling;
            bool comingOutVisibleForPostCulling = __instance.m_comingOutVisibleForPostCulling;

            EOSLogger.Debug("LG_HSUActivator_Core.OnSyncStatusChanged " + newState.status);
            switch (newState.status)
            {
                case eHSUActivatorStatus.WaitingForInsert:
                    __instance.m_insertHSUInteraction.SetActive(true);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, false, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, false, false, true, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StartSequence();                    
                    __instance.m_sequencerInsertItem.StopSequence();
                    __instance.m_sequencerExtractItem.StopSequence();
                    __instance.m_sequencerExtractionDone.StopSequence();
                    break;
                
                case eHSUActivatorStatus.Inserting:    
                    __instance.m_insertHSUInteraction.SetActive(false);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, true, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, false, false, true, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StopSequence();
                    if (!isRecall)
                    {
                        __instance.m_sequencerInsertItem.StartSequence();
                        EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnHSUActivation);
                        var activationScan = def.ChainedPuzzleOnActivationInstance;
                        if (SNet.IsMaster && activationScan != null)
                        {
                            def.ChainedPuzzleOnActivationInstance.AttemptInteract(ChainedPuzzles.eChainedPuzzleInteraction.Activate);
                        }
                    }
                    __instance.m_sequencerExtractItem.StopSequence();
                    __instance.m_sequencerExtractionDone.StopSequence();
                    break;

                case eHSUActivatorStatus.Extracting:
                    __instance.m_insertHSUInteraction.SetActive(false);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, !__instance.m_showItemComingOut, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, __instance.m_showItemComingOut, false, true, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StopSequence();
                    __instance.m_sequencerInsertItem.StopSequence();
                    __instance.m_sequencerExtractItem.StartSequence();
                    __instance.m_sequencerExtractionDone.StopSequence();
                    break;

                case eHSUActivatorStatus.ExtractionDone:
                    __instance.m_insertHSUInteraction.SetActive(false);
                    __instance.ResetItem(__instance.m_itemGoingInAlign, __instance.m_linkedItemGoingIn, !__instance.m_showItemComingOut, false, true, ref goingInVisibleForPostCulling);
                    __instance.ResetItem(__instance.m_itemComingOutAlign, __instance.m_linkedItemComingOut, __instance.m_showItemComingOut, def.TakeOutItemAfterActivation, false, ref comingOutVisibleForPostCulling);
                    __instance.m_sequencerWaitingForItem.StopSequence();
                    __instance.m_sequencerInsertItem.StopSequence();
                    __instance.m_sequencerExtractItem.StopSequence();
                    __instance.m_sequencerExtractionDone.StartSequence();                    
                    if (newState.isSequenceIncomplete)
                    {
                        __instance.HSUInsertSequenceDone();
                    }
                    break;
            }

            return false;
        }
    }
}
