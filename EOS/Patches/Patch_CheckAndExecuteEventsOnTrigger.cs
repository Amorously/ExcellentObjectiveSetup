using GameData;
using HarmonyLib;

namespace EOS.Patches
{
    [HarmonyPatch]
    internal class Patch_CheckAndExecuteEventsOnTrigger
    {        
        [HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckAndExecuteEventsOnTrigger), new Type[] 
        {
            typeof(WardenObjectiveEventData),
            typeof(eWardenObjectiveEventTrigger),
            typeof(bool),
            typeof(float)
        })]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.HigherThanNormal)]
        private static bool Pre_CheckAndExecuteEventsOnTrigger(WardenObjectiveEventData eventToTrigger, eWardenObjectiveEventTrigger trigger, bool ignoreTrigger, float currentDuration)
        {
            if (eventToTrigger == null || !ignoreTrigger && eventToTrigger.Trigger != trigger || currentDuration != 0.0 && eventToTrigger.Delay <= currentDuration)
                return true;

            uint eventID = (uint)eventToTrigger.Type;
            if (!EOSWardenEventManager.HasEventDefinition(eventID)) 
                return true;

            string msg = EOSWardenEventManager.IsVanillaEventID(eventID) ? "overriding vanilla event implementation..." : "executing...";
            EOSLogger.Debug($"EOSWardenEvent: found definition for event ID {eventID}, {msg}");
            EOSWardenEventManager.HandleEvent(eventToTrigger, currentDuration);
            return false;
        }
    }
}
