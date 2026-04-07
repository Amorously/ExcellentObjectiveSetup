using AmorLib.Utils.Extensions;
using EOS.Modules.World.EMP;
using HarmonyLib;
using Player;

namespace EOS.Patches.EMP
{
    [HarmonyPatch]
    internal static class Patch_PlayerAgent_Setup
    {
        
        [HarmonyPatch(typeof(PlayerAgent), nameof(PlayerAgent.Setup))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_Setup(PlayerAgent __instance)
        {
            if (__instance.IsLocallyOwned)
                __instance.gameObject.AddOrGetComponent<PlayerEMPComp>();
        }
    }
}
