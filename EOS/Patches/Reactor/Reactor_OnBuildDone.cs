using AmorLib.Utils;
using EOS.Modules.Instances;
using EOS.Modules.Objectives.Reactor;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.OnBuildDone))]
    internal static class Reactor_OnBuildDone
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_WardenObjective_Reactor_OnBuildDone(LG_WardenObjective_Reactor __instance)
        {
            ReactorInstanceManager.Current.Register(__instance);

            if (__instance.m_isWardenObjective)
            {
                if (!ReactorStartupOverrideManager.Current.TryGetDefinition(__instance, out var def))
                    return;
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
                EOSLogger.Debug($"Reactor Shutdown Instance {def}: custom setup complete");  
            }
        }
    }
}
