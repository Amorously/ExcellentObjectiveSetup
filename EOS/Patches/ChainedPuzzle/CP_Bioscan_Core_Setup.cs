using ChainedPuzzles;
using EOS.Modules.Tweaks.ThermalSights;
using HarmonyLib;

namespace EOS.Patches.ChainedPuzzle
{
    [HarmonyPatch(typeof(CP_Bioscan_Core), nameof(CP_Bioscan_Core.Setup))]
    internal static class CP_Bioscan_Core_Setup
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_CaptureBioscanVisual(CP_Bioscan_Core __instance)
        {
            TSAManager.Current.RegisterPuzzleVisual(__instance);
        }
    }
}
