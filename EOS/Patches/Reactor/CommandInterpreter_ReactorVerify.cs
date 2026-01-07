using HarmonyLib;
using LevelGeneration;
using Localization;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReactorVerify))]
    internal static class CommandInterpreter_ReactorVerify
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_ReactorVerify(LG_ComputerTerminalCommandInterpreter __instance, string param1)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (reactor == null)
            {
                EOSLogger.Error("ReactorVerify: connected reactor is null - bug detected");
                return true;
            }

            if (reactor.m_isWardenObjective) 
                return true;

            if (reactor.ReadyForVerification && param1 == reactor.CurrentStateOverrideCode)
            {
                __instance.m_terminal.ChangeState(TERM_State.ReactorError);
            }
            else
            {
                __instance.AddOutput("");
                __instance.AddOutput(TerminalLineType.SpinningWaitNoDone, Text.Get(2195342028), 4f);
                __instance.AddOutput("");
            }

            return false;
        }
    }
}
