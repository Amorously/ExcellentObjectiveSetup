using EOS.Modules.Tweaks.SecDoorIntText;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.SecurityDoor
{
    [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.Setup))]
    internal static class Patch_SecDoorLocks_Setup
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_Customize_SecDoor_Interaction_Text(LG_SecurityDoor_Locks __instance)
        {
            if (SecDoorIntTextOverrideManager.Current.TryGetDefinition(__instance, out var _)) 
                SecDoorIntTextOverrideManager.Current.RegisterDoorLocks(__instance);
        }
    }
}
