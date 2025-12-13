using EOS.Modules.Objectives.TerminalUplink;
using GameData;
using HarmonyLib;
using LevelGeneration;
using Localization;
namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.StartTerminalUplinkSequence))]
    internal static class StartTerminalUplinkSequence
    {
        // rewrite is indispensable
        // both uplink and corruplink call this method
        // uplink calls on uplink terminal
        // corruplink calls on receiver side
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_StartTerminalUplinkSequence(LG_ComputerTerminalCommandInterpreter __instance, string uplinkIp, bool corrupted)
        {
            var receiver = __instance.m_terminal;
            var terminal = corrupted ? receiver.CorruptedUplinkReceiver : receiver;

            if (terminal.m_isWardenObjective) return true; // vanilla uplink
            if (!UplinkObjectiveManager.Current.TryGetDefinition(terminal, out var uplinkConfig)) return true;

            if (!corrupted)
            {
                terminal.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2583360288), uplinkIp), 3f);
                __instance.TerminalUplinkSequenceOutputs(terminal, false);
            }
            else
            {
                terminal.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2056072887), terminal.PublicName), 3f);
                terminal.m_command.AddOutput("");
                receiver.m_command.AddOutput(TerminalLineType.ProgressWait, string.Format(Text.Get(2056072887), terminal.PublicName), 3f);
                receiver.m_command.AddOutput("");

                receiver.m_command.TerminalUplinkSequenceOutputs(terminal, false);
                receiver.m_command.TerminalUplinkSequenceOutputs(receiver, true);
            }

            receiver.m_command.OnEndOfQueue = new Action(() =>
            {
                EOSLogger.Debug("UPLINK CONNECTION DONE!");

                terminal.UplinkPuzzle.Connected = true;
                terminal.UplinkPuzzle.CurrentRound.ShowGui = true;
                terminal.UplinkPuzzle.OnStartSequence();

                EOSWardenEventManager.ExecuteWardenEvents(uplinkConfig.EventsOnCommence);

                int i = uplinkConfig.RoundOverrides.FindIndex(o => o.RoundIndex == 0);
                UplinkRound firstRoundOverride = i != -1 ? uplinkConfig.RoundOverrides[i] : null!;

                if (firstRoundOverride != null)
                    EOSWardenEventManager.ExecuteWardenEvents(firstRoundOverride.EventsOnRound, eWardenObjectiveEventTrigger.OnStart, false);
            });

            return false;
        }
    }
}
