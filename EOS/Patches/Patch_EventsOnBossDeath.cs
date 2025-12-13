using AmorLib.Utils;
using Enemies;
using EOS.Modules.Tweaks.BossEvents;
using GameData;
using GTFO.API;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches
{
    [HarmonyPatch]
    internal static class Patch_EventsOnBossDeath
    {
        private static readonly HashSet<ushort> _executedForInstances = new(); 

        // called on both host and client side
        
        [HarmonyPatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_SpawnEnemy(EnemySync __instance, pEnemySpawnData spawnData) // 原生怪的mode == hibernate
        {
            if (!spawnData.courseNode.TryGet(out var node) || node == null)
            {
                EOSLogger.Error("Failed to get spawnnode for a boss! Skipped EventsOnBossDeath for it");
                return;
            }

            LG_Zone spawnedZone = node.m_zone;
            var def = BossDeathEventManager.Current.GetDefinition(spawnedZone.ToIntTuple());
            if (def == null) return;

            EnemyAgent enemy = __instance.m_agent;

            if (!def.BossIDs.Contains(enemy.EnemyData.persistentID)) return;

            // TODO: test 
            bool isHibernate = (spawnData.mode == Agents.AgentMode.Hibernate || spawnData.mode == Agents.AgentMode.Scout) && def.ApplyToHibernate;
            bool isAggressive = spawnData.mode == Agents.AgentMode.Agressive && def.ApplyToWave;
            if (!isHibernate && !isAggressive) return;

            var mode = spawnData.mode == Agents.AgentMode.Hibernate ? BossDeathEventManager.Mode.HIBERNATE : BossDeathEventManager.Mode.WAVE;
            //BossDeathEventManager.Current.RegisterInLevelBDEventsExecution(def, mode);

            enemy.add_OnDeadCallback(new Action(() =>
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

                if (!BossDeathEventManager.Current.TryConsumeBDEventsExecutionTimes(def, mode))
                {
                    EOSLogger.Debug($"EventsOnBossDeath: execution times depleted for {def.GlobalZoneIndexTuple()}, {mode}");
                    return;
                }

                ushort enemyID = enemy.GlobalID;
                if (_executedForInstances.Contains(enemyID))
                {
                    _executedForInstances.Remove(enemyID);
                    return;
                }

                def.EventsOnBossDeath.ForEach(e => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(e, eWardenObjectiveEventTrigger.None, true));
                _executedForInstances.Add(enemyID);
            }));

            EOSLogger.Debug($"EventsOnBossDeath: added for enemy with id  {enemy.EnemyData.persistentID}, mode: {spawnData.mode}");
        }

        static Patch_EventsOnBossDeath()
        {
            LevelAPI.OnLevelCleanup += _executedForInstances.Clear;
        }
    }
}
