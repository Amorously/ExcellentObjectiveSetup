using EOS.Modules.Objectives.Reactor;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.OnReactorShutdownVerifyChaosDone))]
    internal static class Reactor_OnReactorShutdownVerifyChaosDone
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_LG_ComputerTerminalCommandInterpreter_OnReactorShutdownVerifyChaosDone(LG_ComputerTerminalCommandInterpreter __instance)
        {
            var reactor = __instance.m_terminal.ConnectedReactor;
            if (reactor == null || reactor.m_isWardenObjective) 
                return true;

            if (!ReactorShutdownObjectiveManager.Current.TryGetDefinition(reactor, out var def))
            {
                EOSLogger.Error("OnReactorShutdownVerifyChaosDone: found built custom reactor shutdown but its definition is missing, what happened?");
                return false;
            }

            reactor.AttemptInteract(def.ChainedPuzzleOnVerificationInstance != null ? eReactorInteraction.Verify_shutdown : eReactorInteraction.Finish_shutdown);

            return false;
        }
    }
}
