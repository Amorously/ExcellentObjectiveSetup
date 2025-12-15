using AmorLib.Utils;
using ChainedPuzzles;
using EOS.Modules.Objectives.TerminalUplink;
using GameData;
using HarmonyLib;
using LevelGeneration;
using Localization;
using SNetwork;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.TerminalUplinkVerify))]
    internal static class TerminalUplinkVerify
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_TerminalUplinkVerify(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2, ref bool __result)
        {
            if (__instance.m_terminal.m_isWardenObjective) // vanilla uplink, corruplink log sent in TerminalUplinkPuzzle
                return true;
            if (!UplinkObjectiveManager.Current.TryGetDefinition(__instance.m_terminal, out var uplinkConfig))
                return true;

            var uplinkPuzzle = __instance.m_terminal.UplinkPuzzle;
            int currentRoundIndex = uplinkPuzzle.m_roundIndex;
            var roundOverride = GetRoundOverride(currentRoundIndex);
            TimeSettings timeSettings = roundOverride != null ? roundOverride.OverrideTimeSettings : uplinkConfig.DefaultTimeSettings;

            float timeToStartVerify = timeSettings.TimeToStartVerify >= 0f ? timeSettings.TimeToStartVerify : uplinkConfig.DefaultTimeSettings.TimeToStartVerify;
            float timeToCompleteVerify = timeSettings.TimeToCompleteVerify >= 0f ? timeSettings.TimeToCompleteVerify : uplinkConfig.DefaultTimeSettings.TimeToCompleteVerify;
            float timeToRestoreFromFail = timeSettings.TimeToRestoreFromFail >= 0f ? timeSettings.TimeToRestoreFromFail : uplinkConfig.DefaultTimeSettings.TimeToRestoreFromFail;

            if (!uplinkPuzzle.Connected) // unconnected
            {
                __instance.AddOutput("");
                __instance.AddOutput(Text.Get(403360908));
                __result = false;
                return false;
            }

            __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(2734004688), timeToStartVerify); // attempting uplink verification...

            if (!uplinkPuzzle.Solved && uplinkPuzzle.CurrentRound.CorrectCode.ToUpper() == param1.ToUpper()) // correct verification
            {
                __instance.AddOutput(string.Format(Text.Get(1221800228), uplinkPuzzle.CurrentProgress)); // verification code {0} correct

                if (uplinkPuzzle.TryGoToNextRound()) // Goto next round
                {
                    int newRoundIndex = uplinkPuzzle.m_roundIndex;
                    var newRoundOverride = GetRoundOverride(newRoundIndex);

                    if (roundOverride != null)
                        EOSWardenEventManager.ExecuteWardenEvents(roundOverride.EventsOnRound, eWardenObjectiveEventTrigger.OnMid, false);

                    if (roundOverride != null && roundOverride.ChainedPuzzleToEndRoundInstance != null) // roundOverride CP 
                    {
                        if (DataBlockUtil.TryGetBlock<TextDataBlock>("InGame.UplinkTerminal.ScanRequiredToProgress", out var block))
                            __instance.AddOutput(TerminalLineType.ProgressWait, Text.Get(block.persistentID));

                        roundOverride.ChainedPuzzleToEndRoundInstance.OnPuzzleSolved += new Action(() =>
                        {
                            __instance.AddOutput(TerminalLineType.ProgressWait, Text.Get(27959760), timeToCompleteVerify); // "Building uplink verification signature"
                            __instance.AddOutput("");
                            __instance.AddOutput(string.Format(Text.Get(4269617288), uplinkPuzzle.CurrentProgress, uplinkPuzzle.CurrentRound.CorrectPrefix));
                            __instance.OnEndOfQueue = CreateNextRoundOnEndAction(newRoundOverride);
                        });

                        if (SNet.IsMaster)
                            roundOverride.ChainedPuzzleToEndRoundInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                    }
                    else // no override CP
                    {
                        __instance.AddOutput(TerminalLineType.ProgressWait, Text.Get(27959760), timeToCompleteVerify); // "Building uplink verification signature"
                        __instance.AddOutput("");
                        __instance.AddOutput(string.Format(Text.Get(4269617288), uplinkPuzzle.CurrentProgress, uplinkPuzzle.CurrentRound.CorrectPrefix));

                        __instance.OnEndOfQueue = CreateNextRoundOnEndAction(newRoundOverride);
                    }
                }
                else // uplink done
                {
                    __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(1780488547), 3f);
                    __instance.AddOutput("");

                    EOSLogger.Error("UPLINK VERIFICATION SEQUENCE DONE!");
                    LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId = 0U;
                    uplinkPuzzle.Solved = true;
                    uplinkPuzzle.OnPuzzleSolved?.Invoke(); // Tested, it's safe to do this
                    UplinkObjectiveManager.Current.ChangeState(__instance.m_terminal, new() { status = UplinkStatus.Finished, currentRoundIndex = uplinkPuzzle.m_roundIndex });

                    if (roundOverride != null && roundOverride.ChainedPuzzleToEndRoundInstance != null) // roundOverride CP
                    {
                        roundOverride.ChainedPuzzleToEndRoundInstance.OnPuzzleSolved += new Action(() =>
                        {
                            __instance.AddOutput(TerminalLineType.Normal, string.Format(Text.Get(3928683780), uplinkPuzzle.TerminalUplinkIP), 2f);
                            __instance.AddOutput("");
                            __instance.OnEndOfQueue = FinalUplinkVerification();
                        });

                        if (SNet.IsMaster)
                            roundOverride.ChainedPuzzleToEndRoundInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                    }
                    else // no override CP
                    {
                        __instance.AddOutput(TerminalLineType.Normal, string.Format(Text.Get(3928683780), uplinkPuzzle.TerminalUplinkIP), 2f);
                        __instance.AddOutput("");

                        FinalUplinkVerification().Invoke();
                    }
                }
            }
            else if (uplinkPuzzle.Solved) // already solved
            {
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.Fail, Text.Get(4080876165));
                __instance.AddOutput(TerminalLineType.Normal, Text.Get(4104839742), 6f);
            }
            else // incorrect verification
            {
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.Fail, string.Format(Text.Get(507647514), uplinkPuzzle.CurrentRound.CorrectPrefix));
                __instance.AddOutput(TerminalLineType.Normal, Text.Get(4104839742), timeToRestoreFromFail);
            }            

            __result = false;
            return false;

            UplinkRound GetRoundOverride(int roundIndex)
            {
                int idx = uplinkConfig!.RoundOverrides.FindIndex(o => o.RoundIndex == roundIndex);
                return idx != -1 ? uplinkConfig.RoundOverrides[idx] : null!;
            }

            Action CreateNextRoundOnEndAction(UplinkRound newRoundOverride)
            {
                return new(() =>
                {
                    EOSLogger.Log("UPLINK VERIFICATION GO TO NEXT ROUND!");
                    uplinkPuzzle.CurrentRound.ShowGui = true;

                    if (newRoundOverride != null)
                        EOSWardenEventManager.ExecuteWardenEvents(newRoundOverride.EventsOnRound, eWardenObjectiveEventTrigger.OnStart, false);

                    UplinkObjectiveManager.Current.ChangeState(__instance.m_terminal, new() { status = UplinkStatus.InProgress, currentRoundIndex = uplinkPuzzle.m_roundIndex });
                });
            }

            Action FinalUplinkVerification()
            {
                return new(() =>
                {
                    EOSLogger.Error("UPLINK VERIFICATION SEQUENCE DONE!");
                    LG_ComputerTerminalManager.OngoingUplinkConnectionTerminalId = 0u;
                    uplinkPuzzle.Solved = true;
                    uplinkPuzzle.OnPuzzleSolved?.Invoke();
                    UplinkObjectiveManager.Current.ChangeState(__instance.m_terminal, new() { status = UplinkStatus.Finished, currentRoundIndex = uplinkPuzzle.m_roundIndex });
                });
            }
        }
    }
}
