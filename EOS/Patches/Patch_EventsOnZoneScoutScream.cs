using AmorLib.Utils;
using Enemies;
using EOS.Modules.Tweaks.Scout;
using GameData;
using HarmonyLib;
using SNetwork;

namespace EOS.Patches
{
    [HarmonyPatch(typeof(ES_ScoutScream), nameof(ES_ScoutScream.CommonUpdate))]
    internal static class Patch_EventsOnZoneScoutScream
    {        
        [HarmonyPrefix]
        private static bool Pre_ES_ScoutScream_CommonUpdate(ES_ScoutScream __instance)
        {
            var enemyAgent = __instance.m_enemyAgent;
            var spawnNode = enemyAgent.CourseNode;

            var def = ScoutScreamEventManager.Current.GetDefinition(spawnNode.m_zone.ToIntTuple());
            if (def == null) return true;

            if (__instance.m_state != ES_ScoutScream.ScoutScreamState.Response || __instance.m_stateDoneTimer >= Clock.Time) return true;

            if (def.EventsOnScoutScream != null && def.EventsOnScoutScream.Count > 0)
            {
                EOSLogger.Debug($"EventsOnZoneScoutScream: found config for {def}, executing events.");
                def.EventsOnScoutScream.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
            }

            if (!def.SuppressVanillaScoutWave)
            {
                if (SNet.IsMaster)
                {
                    if (__instance.m_enemyAgent.CourseNode != null)
                    {
                        if (RundownManager.ActiveExpedition.Expedition.ScoutWaveSettings > 0U && RundownManager.ActiveExpedition.Expedition.ScoutWavePopulation > 0U)
                            Mastermind.Current.TriggerSurvivalWave(__instance.m_enemyAgent.CourseNode, RundownManager.ActiveExpedition.Expedition.ScoutWaveSettings, RundownManager.ActiveExpedition.Expedition.ScoutWavePopulation, out ushort _);
                        else
                            UnityEngine.Debug.LogError("ES_ScoutScream, a scout is screaming but we can't spawn a wave because the the scout settings are not set for this expedition! ScoutWaveSettings: " + RundownManager.ActiveExpedition.Expedition.ScoutWaveSettings + " ScoutWavePopulation: " + RundownManager.ActiveExpedition.Expedition.ScoutWavePopulation);
                    }
                }
            }
            else
            {
                EOSLogger.Debug("Vanilla scout wave suppressed.");
            }

            if (SNet.IsMaster)
            {
                __instance.m_enemyAgent.AI.m_behaviour.ChangeState(EB_States.InCombat);
            }

            __instance.m_machine.ChangeState((int)ES_StateEnum.PathMove);
            __instance.m_state = ES_ScoutScream.ScoutScreamState.Done;

            return false;
        }
    }
}
