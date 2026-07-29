using EOS.Modules.Instances;
using HarmonyLib;
using LevelGeneration;
using SNetwork;

namespace EOS.Patches.Expedition
{
    [HarmonyPatch]
    internal static class TerminalExpeditionPatches
    {
        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.OnProximityEnter))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_ComputerTerminal(LG_ComputerTerminal __instance)
        {
            if (!SNet.IsMaster) return;
            var wrapper = TerminalInstanceManager.Current.GetTerminalWrapper(__instance);
            wrapper?.ChangeState();
        }

        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static void Pre_ReceiveCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            var wrapper = TerminalInstanceManager.Current.GetTerminalWrapper(__instance.m_terminal);
            wrapper?.ReceiveCommand(cmd, param1.ToUpperInvariant());
        }
    }
}
