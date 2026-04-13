using EOS.Modules.World.EMP.Handlers;
using HarmonyLib;
using Player;

namespace EOS.Patches.EMP
{
    [HarmonyPatch]
    internal static class Patch_PlayerSync
    {        
        [HarmonyPatch(typeof(PlayerSync), nameof(PlayerSync.WantsToSetFlashlightEnabled))]
        [HarmonyPrefix]
        [HarmonyAfter("EEC.Harmony")]
        [HarmonyWrapSafe]
        private static void Pre_WantsToSetFlashlightEnabled(ref bool enable)
        {
            if (EMPPlayerFlashlightHandler.Instance?.IsEMPed() == true)            
                enable = false;
        }
    }
}
