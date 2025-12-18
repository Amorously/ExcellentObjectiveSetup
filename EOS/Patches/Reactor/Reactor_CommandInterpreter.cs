using AmorLib.Utils.Extensions;
using ChainedPuzzles;
using EOS.Modules.Objectives.Reactor;
using GameData;
using HarmonyLib;
using LevelGeneration;
using Localization;
using SNetwork;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch]
    internal static class Reactor_CommandInterpreter
    {
        // In vanilla, LG_ComputerTerminalCommandInterpreter.ReactorShutdown() is not used at all
        // So I have to do this shit in this patched method instead
        // I hate you 10cc :)
        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReceiveCommand))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_ReceiveCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, string inputLine, string param1, string param2)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (reactor == null) 
                return true;

            if (cmd == TERM_Command.ReactorShutdown && !reactor.m_isWardenObjective)
            {
                return Handle_ReactorShutdown(__instance, reactor);
            }
            else if (cmd == TERM_Command.UniqueCommand5)
            {
                return Handle_ReactorStartup_SpecialCommand(__instance, cmd, reactor);
            }
            else
            {
                return true;
            }
        }

        private static bool Handle_ReactorShutdown(LG_ComputerTerminalCommandInterpreter __instance, LG_WardenObjective_Reactor reactor)
        {
            if (!ReactorShutdownObjectiveManager.Current.TryGetDefinition(reactor, out var def))
            {
                EOSLogger.Error($"ReactorVerify: found built custom reactor shutdown but its definition is missing, what happened?");
                return true;
            }

            __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(3436726297), 4f);

            if (def.ChainedPuzzleToActiveInstance != null)
            {
                __instance.AddOutput(Text.Get(2277987284));
                if (SNet.IsMaster)
                {
                    def.ChainedPuzzleToActiveInstance.AttemptInteract(eChainedPuzzleInteraction.Activate);
                }
            }
            else
            {
                reactor.AttemptInteract(eReactorInteraction.Initiate_shutdown);
            }

            return false;
        }

        private static bool Handle_ReactorStartup_SpecialCommand(LG_ComputerTerminalCommandInterpreter __instance, TERM_Command cmd, LG_WardenObjective_Reactor reactor)
        {
            if (__instance.m_terminal.CommandIsHidden(cmd)) // cooldown command is hidden
                return true;

            if (!reactor.gameObject.TryAndGetComponent<OverrideReactorComp>(out var component)) 
                return true;

            if (!reactor.ReadyForVerification)
            {
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, ReactorStartupOverrideManager.NotReadyForVerificationOutputText, 4f);
                __instance.AddOutput("");
                return false;
            }

            if (component.IsCorrectTerminal(__instance.m_terminal))
            {
                EOSLogger.Log("Reactor Verify Correct!");

                if (SNet.IsMaster)
                {
                    reactor.AttemptInteract(reactor.m_currentWaveCount == reactor.m_waveCountMax ? eReactorInteraction.Finish_startup : eReactorInteraction.Verify_startup);
                }
                else // execute OnEndEvents on client side 
                {
                    WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(reactor.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnEnd, false);
                }

                __instance.AddOutput(ReactorStartupOverrideManager.CorrectTerminalOutputText);
            }
            else
            {
                EOSLogger.Log("Reactor Verify Incorrect!");
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, ReactorStartupOverrideManager.IncorrectTerminalOutputText, 4f);
                __instance.AddOutput("");
            }

            return false;
        }
    }
}
