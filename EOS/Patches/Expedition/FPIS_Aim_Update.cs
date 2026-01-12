using EOS.Modules.Expedition.ThermalSights;
using FirstPersonItem;
using HarmonyLib;

namespace EOS.Patches.Expedition
{
    [HarmonyPatch(typeof(FPIS_Aim), nameof(FPIS_Aim.Update))]
    internal static class FPIS_Aim_Update
    {
        [HarmonyPostfix]
        private static void Post_Aim_Update(FPIS_Aim __instance)
        {
            if (__instance.Holder.WieldedItem == null) 
                return;

            if (!TSAManager.Current.IsGearWithThermal(TSAManager.Current.CurrentGearPID))
                return;

            float t = 1f - FirstPersonItemHolder.m_transitionDelta;
            TSAManager.Current.SetCurrentThermalSightSettings(t);
            TSAManager.Current.SetPuzzleVisualsIntensity(t);
        }
    }
}
