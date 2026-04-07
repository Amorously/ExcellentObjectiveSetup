using EOS.Modules.World.EMP;
using Gear;
using HarmonyLib;

namespace EOS.Patches.EMP
{
    [HarmonyPatch]
    internal static class EMPEvents
    {        
        [HarmonyPatch(typeof(PlayerInventoryBase), nameof(PlayerInventoryBase.OnItemEquippableFlashlightWielded))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_FlashlightWielded(GearPartFlashlight flashlight)
        {
            EMPManager.FlashlightWielded?.Invoke(flashlight);
        }
        
        [HarmonyPatch(typeof(PlayerInventoryLocal), nameof(PlayerInventoryLocal.DoWieldItem))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_DoWieldItem(PlayerInventoryLocal __instance)
        {
            EMPManager.InventoryWielded?.Invoke(__instance.WieldedSlot);
        }
    }
}
