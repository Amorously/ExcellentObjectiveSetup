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
            if (terminal.m_isWardenObjective) // vanilla uplink
                return true;

            bool hasDef = UplinkObjectiveManager.Current.TryGetDefinition(terminal, out var uplinkConfig);
            bool receiverHasDef = terminal.CorruptedUplinkReceiver != null && UplinkObjectiveManager.Current.TryGetDefinition(terminal.CorruptedUplinkReceiver, out uplinkConfig);
            if (!hasDef && !receiverHasDef || uplinkConfig == null) // `terminal` is either sender or receiver 
                return true;

            if (!UplinkObjectiveManager.Current.FirstRoundOutputted(terminal))
            {
                terminal.m_command.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(3418104670), 3f);
                terminal.m_command.AddOutput("");

                if (uplinkConfig.DisplayUplinkWarning)
                {
                    terminal.m_command.AddOutput(TerminalLineType.Warning, "WARNING! Breach detected!", 0.8f);
                    terminal.m_command.AddOutput(TerminalLineType.Warning, "WARNING! Breach detected!", 0.8f);
                    terminal.m_command.AddOutput(TerminalLineType.Warning, "WARNING! Breach detected!", 0.8f);
                    terminal.m_command.AddOutput("");
                }

                if (!corrupted)
                {
                    terminal.m_command.AddOutput(string.Format(Text.Get(947485599), terminal.UplinkPuzzle.CurrentRound.CorrectPrefix));
                }

                UplinkObjectiveManager.Current.ChangeState(terminal, new(), true);
            }
            return false;
        }
    }
}
