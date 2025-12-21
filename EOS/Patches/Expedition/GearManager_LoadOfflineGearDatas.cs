using Gear;
using HarmonyLib;
using EOS.Modules.Expedition.Gears;
using EOS.Modules.Tweaks.ThermalSights;

namespace EOS.Patches.Expedition
{
    [HarmonyPatch(typeof(GearManager), nameof(GearManager.LoadOfflineGearDatas))]
    internal static class GearManager_LoadOfflineGearDatas // called on both host and client side
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_GearManager_LoadOfflineGearDatas(GearManager __instance)
        {
            ExpeditionGearManager.Current.VanillaGearManager = __instance;

            foreach (var (inventorySlot, loadedGears) in ExpeditionGearManager.Current.GearSlots)
            {
                foreach (GearIDRange gearIDRange in __instance.m_gearPerSlot[(int)inventorySlot])
                {
                    uint playerOfflineDBPID = ExpeditionGearManager.GetOfflineGearPID(gearIDRange);
                    loadedGears.Add(playerOfflineDBPID, gearIDRange);
                }
            }

            TSAManager.Current.InitThermalOfflineGears();
        }
    }
}
