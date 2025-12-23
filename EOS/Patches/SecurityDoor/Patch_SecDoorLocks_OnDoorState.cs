using EOS.Modules.Tweaks.SecDoorIntText;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.SecurityDoor
{
    [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.OnDoorState))]
    internal static class Patch_SecDoorLocks_OnDoorState
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Normal)]
        [HarmonyWrapSafe]
        private static void Patch_OnDoorState(LG_SecurityDoor_Locks __instance, pDoorState state)
        {
            if (state.status != eDoorStatus.Closed_LockedWithChainedPuzzle && state.status != eDoorStatus.Closed_LockedWithChainedPuzzle_Alarm)
                return;

            SecDoorIntTextOverrideManager.Current.ReplaceText(__instance);
        }
    }
}
