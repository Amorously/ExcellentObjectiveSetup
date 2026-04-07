using AmorLib.Utils.Extensions;
using EOS.Modules.World.EMP;
using EOS.Modules.World.EMP.Handlers;
using HarmonyLib;

namespace EOS.Patches.EMP
{
    [HarmonyPatch]
    internal static class Patch_SentryGunInstance
    {
        [HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.Setup))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_Setup(SentryGunInstance __instance) // IMPORTANT NOTE: this patch is called every time the sentry gun is placed on the ground
        {
            __instance.gameObject.AddOrGetComponent<EMPController>().AssignHandler(new EMPSentryHandler());
        }
    }
}
