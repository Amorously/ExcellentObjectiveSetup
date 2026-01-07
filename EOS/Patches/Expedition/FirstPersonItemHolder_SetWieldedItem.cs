using EOS.Modules.Expedition.ThermalSights;
using HarmonyLib;

namespace EOS.Patches.Expedition
{
    [HarmonyPatch(typeof(FirstPersonItemHolder), nameof(FirstPersonItemHolder.SetWieldedItem))]
    internal static class FirstPersonItemHolder_SetWieldedItem
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_SetWieldedItem(ItemEquippable item)
        {
            TSAManager.Current.OnPlayerItemWielded(item);
            TSAManager.Current.SetCurrentThermalSightSettings(1f);
        }
    }
}
