using EOS.Modules.Instances;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Terminal
{
    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
    internal static class Patch_LG_ComputerTerminal_Setup
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminal_Setup(LG_ComputerTerminal __instance)
        {
            TerminalInstanceManager.Current.Register(__instance);
        }
    }
}
