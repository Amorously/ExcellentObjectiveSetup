using EOS.Modules.Instances;
using EOS.Modules.Objectives.Reactor;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
    internal static class Reactor_OnBuildDone
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static void Pre_LG_WardenObjective_Reactor_OnBuildDone(LG_WardenObjective_Reactor __instance) // allow more than 20 reactor waves
        {
            if (!WardenObjectiveManager.TryGetWardenObjectiveDataForLayer(__instance.SpawnNode.LayerType, __instance.WardenObjectiveChainIndex, out var data) || data == null)
                return;

            int count = __instance.m_overrideCodes.Count; // should be 20
            string[] overrideCodes = new string[Math.Max(count, data.ReactorWaves.Count)];
            for (int i = 0; i < overrideCodes.Length; i++)
            {
                if (i < count)
                {
                    overrideCodes[i] = __instance.m_overrideCodes[i];
                }
                else
                {
                    overrideCodes[i] = SerialGenerator.GetCodeWord();
                }
            }
            __instance.m_overrideCodes = overrideCodes;
        }

        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_WardenObjective_Reactor_OnBuildDone(LG_WardenObjective_Reactor __instance)
        {
            ReactorInstanceManager.Current.Register(__instance);

            if (__instance.m_isWardenObjective)
            {
                if (!ReactorStartupOverrideManager.Current.TryGetDefinition(__instance, out var def))
                {
                    return; // no startup override def
                }
                if (!WardenObjectiveManager.TryGetWardenObjectiveDataForLayer(__instance.SpawnNode.LayerType, __instance.WardenObjectiveChainIndex, out var data) || data == null)
                {
                    EOSLogger.Error("Failed to get WardenObjectiveData for this reactor");
                    return;
                }
                if (data.Type != eWardenObjectiveType.Reactor_Startup)
                {
                    EOSLogger.Error($"Reactor Instance {def} is not setup as vanilla ReactorStartup, cannot override");
                    return;
                }
                def.ObjectiveDB = data;
                ReactorStartupOverrideManager.Build(__instance, def);
            }
            else if (ReactorShutdownObjectiveManager.Current.TryGetDefinition(__instance, out var def))
            {
                ReactorShutdownObjectiveManager.Build(__instance, def);
            }
            else
            {
                EOSLogger.Error("EOS Reactor: something went wrong!");
            }

            if (__instance.m_terminal != null)
                TerminalInstanceManager.Current.Register(__instance.m_terminal); // idk why but reactor terminal doesn't register itself
        }
    }
}
