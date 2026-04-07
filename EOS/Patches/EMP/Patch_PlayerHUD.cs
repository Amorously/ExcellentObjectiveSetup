using EOS.Modules.World.EMP.Handlers;
using HarmonyLib;

namespace EOS.Patches.EMP
{
    [HarmonyPatch]
    internal static class Patch_PlayerHUD
    {
        [HarmonyPatch(typeof(PlayerGuiLayer), nameof(PlayerGuiLayer.UpdateGUIElementsVisibility))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_UpdateGUIElementsVisibility()
        {
            return EMPPlayerHudHandler.Instance?.IsEMPed() != true;
        }
        
        [HarmonyPatch(typeof(CellSettingsApply), nameof(CellSettingsApply.ApplyPlayerGhostOpacity))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static void Pre_ApplyPlayerGhostOpacity(ref float value)
        {
            if (EMPPlayerHudHandler.Instance?.IsEMPed() == true)
                value = 0f;
        }
        
        [HarmonyPatch(typeof(CellSettingsApply), nameof(CellSettingsApply.ApplyHUDAlwaysShowTeammateInfo))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static void Pre_ApplyHUDAlwaysShowTeammateInfo(ref bool value)
        {
            if (EMPPlayerHudHandler.Instance?.IsEMPed() == true)
                value = false;
        }
    }
}
