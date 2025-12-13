using GameData;
using HarmonyLib;
using EOS.Modules.Expedition.Gears;

namespace EOS.Patches.Expedition
{
    [HarmonyPatch(typeof(RundownManager), nameof(RundownManager.SetActiveExpedition))]
    internal static class RundownManager_SetActiveExpedition
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_RundownManager_SetActiveExpedition(RundownManager __instance, pActiveExpedition expPackage, ExpeditionInTierData expTierData)
        {
            if (expPackage.tier == eRundownTier.Surface) return;
            ExpeditionGearManager.Current.SetupAllowedGearsForActiveExpedition();
        }
    }
}
