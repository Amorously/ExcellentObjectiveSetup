using AmorLib.Utils;
using Enemies;
using EOS.Modules.Tweaks.Scout;
using HarmonyLib;
using SNetwork;

namespace EOS.Patches
{
    [HarmonyPatch(typeof(ES_ScoutScream), nameof(ES_ScoutScream.CommonUpdate))]
    internal static class Patch_EventsOnZoneScoutScream
    {
        private static uint ScoutWaveSettings => RundownManager.ActiveExpedition.Expedition.ScoutWaveSettings;
        private static uint ScoutWavePopulation => RundownManager.ActiveExpedition.Expedition.ScoutWavePopulation;

        [HarmonyPrefix]
        private static bool Pre_ES_ScoutScream_CommonUpdate(ES_ScoutScream __instance)
        {
            if (__instance.m_state != ES_ScoutScream.ScoutScreamState.Response || __instance.m_stateDoneTimer >= Clock.Time)
                return true;

            var enemyAgent = __instance.m_enemyAgent;
            var spawnNode = enemyAgent.CourseNode;

            if (!ScoutScreamEventManager.Current.TryGetDefinition(spawnNode.m_zone.ToIntTuple(), out var def)) 
                return true;

            if (def.EventsOnScoutScream != null && def.EventsOnScoutScream.Count > 0)
            {
                EOSLogger.Debug($"EventsOnZoneScoutScream: found config for {def}, executing events.");
                EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnScoutScream);
            }

            if (SNet.IsMaster)
            {
                if (!def.SuppressVanillaScoutWave)
                {
                    if (spawnNode != null && ScoutWaveSettings > 0u && ScoutWavePopulation > 0u)
                        Mastermind.Current.TriggerSurvivalWave(spawnNode, ScoutWaveSettings, ScoutWavePopulation, out ushort _);
                    else
                        EOSLogger.Error($"ES_ScoutScream, a scout is screaming but we can't spawn a wave because the the scout settings are not set for this expedition, or null node! ScoutWaveSettings: {ScoutWaveSettings} ScoutWavePopulation: {ScoutWavePopulation}");
                }
                __instance.m_enemyAgent.AI.m_behaviour.ChangeState(EB_States.InCombat);
            }
            __instance.m_machine.ChangeState((int)ES_StateEnum.PathMove);
            __instance.m_state = ES_ScoutScream.ScoutScreamState.Done;

            return false;
        }
    }
}
