using EOS.Modules.Instances;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Terminal
{
    [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.SetupCommandEvents))]
    internal static class Patch_RepeatableCommandEventFix
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_ResetRepeatableUniqueCommandChainedPuzzle(LG_ComputerTerminalCommandInterpreter __instance)
        {
            var terminal = __instance.m_terminal;

            foreach(var cmd in TerminalInstanceManager.UNIQUE_CMDS)
            {
                if(!__instance.m_commandsPerEnum.ContainsKey(cmd)) // vanilla use UniqueCommands only in order, but anyway we iterate through instead of just return
                    continue;

                var cmdName = __instance.m_commandsPerEnum[cmd];
                if (__instance.m_terminal.GetCommandRule(cmd) != TERM_CommandRule.Normal) continue;

                var cmdEvents = __instance.m_terminal.GetUniqueCommandEvents(cmdName);
                for(int i = 0; i < cmdEvents.Count; i++)
                {
                    if (cmdEvents[i].ChainPuzzle == 0) continue;

                    if (__instance.m_terminal.TryGetChainPuzzleForCommand(cmd, i, out var cpInstance) && cpInstance != null)
                    {
                        cpInstance.OnPuzzleSolved += new Action(cpInstance.ResetProgress);
                    }

                    EOSLogger.Debug($"TerminalTweak: {terminal.ItemKey}, command {cmdName} set to be repeatable!");
                }
            }
        }
    }
}
