using EOS.Modules.Tweaks.ThermalSights;
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

            float t = 1.0f - FirstPersonItemHolder.m_transitionDelta;
            if (TSAManager.Current.IsGearWithThermal(TSAManager.Current.CurrentGearPID))
            {
                TSAManager.Current.SetCurrentThermalSightSettings(t);
            }
            else
            {
                t = Math.Max(0.05f, t);
            }

            TSAManager.Current.SetPuzzleVisualsIntensity(t);
        }
    }
}
