using EOS.Modules.Tweaks.SecDoorIntText;
using HarmonyLib;

namespace EOS.Patches.SecurityDoor
{
    [HarmonyPatch]
    internal class Patch_HandleInteractGlitch
    {
        [HarmonyPatch(typeof(Interact_Timed), nameof(Interact_Timed.OnSelectedChange))]
        [HarmonyPostfix]
        private static void Post_Interact_Timed_OnSelectedChange(Interact_Timed __instance, bool selected)
        {
            Handle(__instance, selected, true);
        }

        [HarmonyPatch(typeof(Interact_MessageOnScreen), nameof(Interact_MessageOnScreen.OnSelectedChange))]
        [HarmonyPostfix]
        private static void Post_Interact_MessageOnScreen_OnSelectedChange(Interact_MessageOnScreen __instance, bool selected)
        {
            Handle(__instance, selected, false);
        }

        private static void Handle(Interact_Base interact, bool selected, bool canInteract)
        {
            if (!selected)
                SecDoorIntTextOverrideManager.Current.AttemptInteractGlitch(interact);
            else
                SecDoorIntTextOverrideManager.Current.AttemptInteractGlitch(interact, canInteract, true);
        }
    }
}
