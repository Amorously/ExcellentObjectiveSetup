using EOS.Modules.Objectives.TerminalUplink;
using GameData;
using HarmonyLib;
using LevelGeneration;
using Localization;
namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.StartTerminalUplinkSequence))]
    internal static class StartTerminalUplinkSequence // both uplink and corruplink call this method. uplink calls on uplink terminal, corruplink calls on receiver side
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_StartTerminalUplinkSequence(LG_ComputerTerminalCommandInterpreter __instance, string uplinkIp, bool corrupted)
        {
            var receiver = __instance.m_terminal;
            var terminal = corrupted ? receiver.CorruptedUplinkReceiver : receiver;

            if (terminal.m_isWardenObjective || !UplinkObjectiveManager.Current.TryGetDefinition(terminal, out var uplinkConfig))
                return true;

            if (uplinkConfig.FirstRoundOutputted(receiver.UplinkPuzzle.m_roundIndex))
            {
                if (!corrupted)
                {
                    terminal.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2583360288), uplinkIp), 3f);
                    UplinkSequenceOutputs(terminal, false);
                }
                else
                {
                    terminal.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2056072887), terminal.PublicName), 3f);
                    terminal.m_command.AddOutput("");
                    receiver.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2056072887), terminal.PublicName), 3f);
                    receiver.m_command.AddOutput("");
                    UplinkSequenceOutputs(terminal, false);
                    UplinkSequenceOutputs(receiver, true);
                }
            }
            
            receiver.m_command.OnEndOfQueue = new Action(() =>
            {
                EOSLogger.Log("UPLINK CONNECTED, VERIFICATION START!");
                UplinkObjectiveManager.Current.ChangeState(terminal, new() { status = UplinkStatus.InProgress });
                terminal.UplinkPuzzle.OnStartSequence();
                EOSWardenEventManager.ExecuteWardenEvents(uplinkConfig.EventsOnCommence);
                int i = uplinkConfig.RoundOverrides.FindIndex(o => o.RoundIndex == 0);
                var firstRoundOverride = i != -1 ? uplinkConfig.RoundOverrides[i] : null;
                if (firstRoundOverride != null)
                    EOSWardenEventManager.ExecuteWardenEvents(firstRoundOverride.EventsOnRound, eWardenObjectiveEventTrigger.OnStart, false);
            });

            return false;

            void UplinkSequenceOutputs(LG_ComputerTerminal outputTerminal, bool corrupted)
            {
                outputTerminal.m_command.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(3418104670), 3f);
                outputTerminal.m_command.AddOutput("");

                if (uplinkConfig.DisplayUplinkWarning)
                {
                    outputTerminal.m_command.AddOutput(TerminalLineType.Warning, "WARNING! Breach detected!", 0.8f);
                    outputTerminal.m_command.AddOutput(TerminalLineType.Warning, "WARNING! Breach detected!", 0.8f);
                    outputTerminal.m_command.AddOutput(TerminalLineType.Warning, "WARNING! Breach detected!", 0.8f);
                    outputTerminal.m_command.AddOutput("");
                }
                if (!corrupted)
                {
                    outputTerminal.m_command.AddOutput(string.Format(Text.Get(947485599), outputTerminal.UplinkPuzzle.CurrentRound.CorrectPrefix));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminalCommandInterpreter_StartTerminalUplinkSequence(LG_ComputerTerminalCommandInterpreter __instance, bool corrupted)
        {
            var receiver = __instance.m_terminal;
            var terminal = corrupted ? receiver.CorruptedUplinkReceiver : receiver;

            if (!terminal.m_isWardenObjective) return; // not vanilla uplink

            var uplinkConfig = UplinkObjectiveManager.Current.GetWardenDefinition(terminal);
            if (uplinkConfig == null) return;

            receiver.m_command.OnEndOfQueue += new Action(() =>
            {
                EOSWardenEventManager.ExecuteWardenEvents(uplinkConfig.EventsOnCommence);
                int i = uplinkConfig.RoundOverrides.FindIndex(o => o.RoundIndex == 0);
                var firstRoundOverride = i != -1 ? uplinkConfig.RoundOverrides[i] : null;
                if (firstRoundOverride != null)
                    EOSWardenEventManager.ExecuteWardenEvents(uplinkConfig.RoundOverrides[i].EventsOnRound, eWardenObjectiveEventTrigger.OnStart, false);
            });
        }
    }
}
