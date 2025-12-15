using EOS.Modules.Instances;
using EOS.Modules.Objectives.Reactor;
using GameData;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnStateCountUpdate))]
    internal static class Reactor_OnStateCountUpdate
    {
        [HarmonyPrefix]
        private static bool Pre_LG_WardenObjective_Reactor_OnStateCountUpdate(LG_WardenObjective_Reactor __instance, int count)
        {
            if (__instance.m_isWardenObjective || ReactorInstanceManager.Current.IsStartupReactor(__instance))
                return true;
            if (!ReactorInstanceManager.Current.IsShutdownReactor(__instance))
            {
                EOSLogger.Error($"Reactor_OnStateCountUpdate: found built custom reactor but it's neither a startup nor shutdown reactor, what happen?");
                return true;
            }            
            if (!ReactorShutdownObjectiveManager.Current.TryGetDefinition(__instance, out var def))
            {
                EOSLogger.Error($"Reactor_OnStateCountUpdate: found built custom reactor but its definition is missing, what happened?");
                return true;
            }

            __instance.m_currentWaveCount = count; // count == 1

            LG_ComputerTerminal? terminal = null;
            if (def.PutVerificationCodeOnTerminal)
                terminal = TerminalInstanceManager.Current.GetInstance(def.VerificationCodeTerminal.IntTuple, def.VerificationCodeTerminal.InstanceIndex);

            __instance.m_currentWaveData = new ReactorWaveData
            {
                HasVerificationTerminal = terminal != null,
                VerificationTerminalSerial = terminal?.ItemKey ?? string.Empty,
                Warmup = 1.0f,
                WarmupFail = 1.0f,
                Wave = 1.0f,
                Verify = 1.0f,
                VerifyFail = 1.0f,
            };

            if (__instance.m_overrideCodes != null && !string.IsNullOrEmpty(__instance.m_overrideCodes[0]))
            {
                __instance.CurrentStateOverrideCode = __instance.m_overrideCodes[0];
            }
            else
            {
                EOSLogger.Error("Reactor_OnStateCountUpdate: code is not built?");
            }

            return false;           
        }
    }
}
