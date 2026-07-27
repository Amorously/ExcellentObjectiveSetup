using EOS.Modules.Expedition;
using EOS.Modules.Instances;
using HarmonyLib;
using LevelGeneration;
using SNetwork;

namespace EOS.Patches.Expedition
{
    [HarmonyPatch]
    internal static class TerminalExpeditionPatches
    {
        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.OnProximityEnter))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_ComputerTerminal(LG_ComputerTerminal __instance)
        {
            if (!SNet.IsMaster) return;
            var wrapper = TerminalInstanceManager.Current.GetTerminalWrapper(__instance);
            wrapper.ChangeState();
        }

        [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.EvaluatePassword))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminal_EvaluatePassword(LG_ComputerTerminal __instance, bool __result)
        {
            if (ExpeditionDefinitionManager.Current.TryGetTerminalDefinitionFromInstance(__instance, out var defs))
            {
                foreach (var def in defs)
                {
                    EOSWardenEventManager.ExecuteWardenEvents(__result ? def.EventsOnPasswordInputSuccess : def.EventsOnPasswordInputFailure);
                }
            }
        }

        [HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.ReadLog))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_ReadLog(LG_ComputerTerminalCommandInterpreter __instance, string param1, string param2)
        {
            if (!SNet.IsMaster) return;            
            var wrapper = TerminalInstanceManager.Current.GetTerminalWrapper(__instance.m_terminal);
            wrapper.ChangeState(param1.ToUpperInvariant());            
        }
    }
}
