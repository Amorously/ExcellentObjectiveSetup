using ChainedPuzzles;
using EOS.Modules.Objectives.TerminalUplink;
using HarmonyLib;
using LevelGeneration;
using Localization;
using SNetwork;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalCorruptedUplinkConfirm))]
    internal static class CorruptedUplinkConfirm 
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalCorruptedUplinkConfirm(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2, ref bool __result)
        {
            // invoked on receiver side
            var receiver = __instance.m_terminal;
            var sender = __instance.m_terminal.CorruptedUplinkReceiver;

            if (sender == null)
            {
                EOSLogger.Error("TerminalCorruptedUplinkConfirm: critical failure because terminal does not have a CorruptedUplinkReceiver (sender).");
                __result = false;
                return false;
            }

            if (sender.m_isWardenObjective) return true; // vanilla uplink

            receiver.m_command.AddOutput(TerminalLineType.Normal, string.Format(Text.Get(2816126705), sender.PublicName));
            // vanilla code in this part is totally brain-dead
            if (sender.ChainedPuzzleForWardenObjective != null)
            {
                sender.ChainedPuzzleForWardenObjective.OnPuzzleSolved += new Action(() => 
                {
                    receiver.m_command.StartTerminalUplinkSequence(string.Empty, true);
                    UplinkObjectiveManager.Current.ChangeState(sender, new() { status = UplinkStatus.InProgress });
                });
                sender.m_command.AddOutput(string.Empty);
                sender.m_command.AddOutput(Text.Get(3268596368));
                sender.m_command.AddOutput(Text.Get(2277987284));

                receiver.m_command.AddOutput(string.Empty);
                receiver.m_command.AddOutput(Text.Get(3268596368));
                receiver.m_command.AddOutput(Text.Get(2277987284));

                if (SNet.IsMaster)
                {
                    sender.ChainedPuzzleForWardenObjective.AttemptInteract(eChainedPuzzleInteraction.Activate);
                }
            }
            else
            {
                receiver.m_command.StartTerminalUplinkSequence(string.Empty, true);
                UplinkObjectiveManager.Current.ChangeState(sender, new() { status = UplinkStatus.InProgress });
            }

            __result = true;
            return false;
        }
    }
}
