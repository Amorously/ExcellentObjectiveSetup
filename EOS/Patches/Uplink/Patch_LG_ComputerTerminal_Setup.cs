using EOS.Modules.Instances;
using EOS.Modules.Tweaks.TerminalPosition;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
    internal static class Patch_LG_ComputerTerminal_Setup
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminal_Setup(LG_ComputerTerminal __instance)
        {
            TerminalInstanceManager.Current.Register(__instance);
            if (__instance.SpawnNode == null) return;
            TerminalPositionOverrideManager.Current.Setup(__instance);
        }
    }
}
