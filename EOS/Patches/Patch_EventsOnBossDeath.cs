using AmorLib.Utils;
using Enemies;
using EOS.Modules.Tweaks.BossEvents;
using GTFO.API;
using HarmonyLib;

namespace EOS.Patches
{
    [HarmonyPatch]
    internal static class Patch_EventsOnBossDeath
    {
        private static readonly HashSet<ushort> _executedForInstances = new();

        static Patch_EventsOnBossDeath()
        {
            LevelAPI.OnLevelCleanup += _executedForInstances.Clear;
        }


        [HarmonyPatch(typeof(EnemySync), nameof(EnemySync.OnSpawn))] 
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_SpawnEnemy(EnemySync __instance, pEnemySpawnData spawnData) // called on both host and client side
        {
            if (!spawnData.courseNode.TryGet(out var node) || node == null) // 原生怪的mode == hibernate
            {
                EOSLogger.Error("Failed to get spawnnode for a boss! Skipped EventsOnBossDeath for it");
                return;
            }

            if (!BossDeathEventManager.Current.TryGetDefinition(node.m_zone.ToIntTuple(), out var def)) 
                return;

            var enemy = __instance.m_agent;
            if (!def.BossIDs.Contains(enemy.EnemyData.persistentID)) 
                return;

            // TODO: test 
            bool isHibernate = (spawnData.mode == Agents.AgentMode.Hibernate || spawnData.mode == Agents.AgentMode.Scout) && def.ApplyToHibernate;
            bool isAggressive = spawnData.mode == Agents.AgentMode.Agressive && def.ApplyToWave;
            if (!isHibernate && !isAggressive) 
                return;

            var mode = spawnData.mode == Agents.AgentMode.Hibernate ? BossDeathEventManager.Mode.HIBERNATE : BossDeathEventManager.Mode.WAVE;
            ushort enemyID = enemy.GlobalID;
            enemy.add_OnDeadCallback(new Action(() =>
            {
                if (GameStateManager.CurrentStateName != eGameStateName.InLevel) 
                    return;
                if (!BossDeathEventManager.Current.TryConsumeBDEventsExecutionTimes(def, mode))
                {
                    EOSLogger.Debug($"EventsOnBossDeath: execution times depleted for {def}, {mode}");
                    return;
                }

                if (_executedForInstances.Contains(enemyID))
                {
                    _executedForInstances.Remove(enemyID);
                    return;
                }

                EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnBossDeath);
                _executedForInstances.Add(enemyID);
            }));

            EOSLogger.Debug($"EventsOnBossDeath: added for enemy with id  {enemy.EnemyData.persistentID}, mode: {spawnData.mode}");
        }
    }
}
