using EOS.Modules.Objectives.TerminalUplink;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalUplinkSequenceOutputs))]
    internal static class TerminalUplinkSequenceOutput
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalUplinkSequenceOutputs(LG_ComputerTerminal terminal, bool corrupted)
        {
            if (terminal.m_isWardenObjective) return true; // vanilla uplink

            // `terminal` is either sender or receiver 
            if (!UplinkObjectiveManager.Current.TryGetDefinition(terminal, out _)
                || terminal.CorruptedUplinkReceiver == null
                || !UplinkObjectiveManager.Current.TryGetDefinition(terminal.CorruptedUplinkReceiver, out var uplinkConfig)
                || uplinkConfig.DisplayUplinkWarning)
            {
                return true;
            }

            terminal.m_command.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(3418104670), 3f);
            terminal.m_command.AddOutput("");

            if (!corrupted)
            {
                terminal.m_command.AddOutput(string.Format(Text.Get(947485599), terminal.UplinkPuzzle.CurrentRound.CorrectPrefix));
            }

            return false;
        }
    }
}
