using EOS.Modules.Tweaks.ThermalSights;
using HarmonyLib;

namespace EOS.Patches.Expedition
{
    [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.SetWieldedItem))]
    internal static class FirstPersonItemHolder_SetWieldedItem
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_SetWieldedItem(FirstPersonItemHolder __instance, ItemEquippable item)
        {
            TSAManager.Current.OnPlayerItemWielded(item);
            TSAManager.Current.SetPuzzleVisualsIntensity(1f);
            TSAManager.Current.SetCurrentThermalSightSettings(1f);
        }
    }
}
