using EOS.Modules.Tweaks.SecDoorIntText;
using HarmonyLib;
using LevelGeneration;
using System.Text;

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

            if (!SecDoorIntTextOverrideManager.Current.TryGetDefinition(__instance, out var def))
                return;

            if (def.ActiveTextOverrideWhitelist.Any() && !def.ActiveTextOverrideWhitelist.Contains(__instance.m_lastStatus))
                return;

            StringBuilder sb = new();
            if (!string.IsNullOrEmpty(def.Prefix))
            {
                sb.Append(def.Prefix).AppendLine();
            }

            var intOpenDoor = __instance.m_intOpenDoor;
            string textToReplace = string.IsNullOrEmpty(def.TextToReplace) ? intOpenDoor.InteractionMessage : def.TextToReplace;
            sb.Append(textToReplace);
            
            if (!string.IsNullOrEmpty(def.Postfix))
            {
                sb.AppendLine().Append(def.Postfix);
            }

            intOpenDoor.InteractionMessage = sb.ToString();
        }
    }
}
