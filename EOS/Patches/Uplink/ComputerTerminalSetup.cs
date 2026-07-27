using EOS.Modules.Instances;
using EOS.Modules.Objectives.TerminalUplink;
using EOS.Modules.Tweaks.TerminalPosition;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch]
    internal static class ComputerTerminalSetup
    {
        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminal_Setup(LG_ComputerTerminal __instance)
        {
            TerminalInstanceManager.Current.Register(__instance);
            if (__instance.SpawnNode == null) return;
            TerminalPositionOverrideManager.Current.Setup(__instance);
        }

        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.SetupAsWardenObjectiveTerminalUplink))]
        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.SetupAsWardenObjectiveCorruptedTerminalUplink))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminal_UplinkSetup(LG_ComputerTerminal __instance)
        {
            TerminalInstanceManager.Current.RegisterWardenUplink(__instance);
        }
        
        [HarmonyPatch(typeof(TerminalUplinkPuzzle), nameof(TerminalUplinkPuzzle.Setup))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminal_SyncedPuzzle(TerminalUplinkPuzzle __instance, LG_ComputerTerminal terminal)
        {
            if (!terminal.m_isWardenObjective) return;

            var def = UplinkObjectiveManager.Current.GetWardenDefinition(terminal);
            if (def == null) return;

            __instance.OnPuzzleSolved += new Action(() =>
            {
                if (def.EventsOnComplete != null)
                    EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnComplete);
            });
        }
    }
}
