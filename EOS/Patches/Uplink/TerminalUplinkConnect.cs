using ChainedPuzzles;
using EOS.Modules.Objectives.TerminalUplink;
using HarmonyLib;
using LevelGeneration;
using Localization;
using SNetwork;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalUplinkConnect))]
    internal static class TerminalUplinkConnect // normal uplink: rewrite the method to do more things
    {        
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalUplinkConnect(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2, ref bool __result)
        {
            var uplinkTerminal = __instance.m_terminal;

            if (uplinkTerminal.m_isWardenObjective) return true; // vanilla uplink

            if (LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId != 0u && LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId != uplinkTerminal.SyncID)
            {
                __instance.AddOngoingUplinkOutput();
                return false;
            }

            if (!UplinkObjectiveManager.Current.TryGetDefinition(uplinkTerminal, out var uplinkConfig)) 
                return true;

            if (!uplinkConfig.UseUplinkAddress)
            {
                param1 = __instance.m_terminal.UplinkPuzzle.TerminalUplinkIP;
            }

            if (!uplinkConfig.UseUplinkAddress || param1 == __instance.m_terminal.UplinkPuzzle.TerminalUplinkIP)
            {
                __instance.m_terminal.TrySyncSetCommandRule(TERM_Command.TerminalUplinkConnect, TERM_CommandRule.OnlyOnce);
                if (__instance.m_terminal.ChainedPuzzleForWardenObjective != null)
                {
                    __instance.m_terminal.ChainedPuzzleForWardenObjective.OnPuzzleSolved += new Action(() => __instance.StartTerminalUplinkSequence(param1));
                    __instance.AddOutput("");
                    __instance.AddOutput(Text.Get(3268596368));
                    if (SNet.IsMaster)
                    {
                        __instance.m_terminal.ChainedPuzzleForWardenObjective.AttemptInteract(eChainedPuzzleInteraction.Activate);
                    }
                }
                else
                    __instance.StartTerminalUplinkSequence(param1);
                __result = true;
            }
            else
            {
                __instance.AddUplinkWrongAddressError(param1);
                __result = false;
            }

            return false;
        }
    }
}
