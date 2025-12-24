using AmorLib.Utils.Extensions;
using EOS.BaseClasses;
using System.Collections.Concurrent;

namespace EOS.Modules.Tweaks.BossEvents
{
    internal sealed class BossDeathEventManager : ZoneDefinitionManager<EventsOnZoneBossDeath, BossDeathEventManager>
    {
        public enum Mode
        { 
            HIBERNATE,
            WAVE
        }

        public const int UNLIMITED_COUNT = int.MaxValue;

        protected override string DEFINITION_NAME => "EventsOnBossDeath";
        
        private readonly ConcurrentDictionary<(int, int, int), EventsOnZoneBossDeath> _levelBDEs = new();

        protected override void OnBuildStart()
        {
            OnLevelCleanup();
            SetupForCurrentExpedition();
        }

        protected override void OnLevelCleanup()
        {
            _levelBDEs.ForEachValue(bde =>  bde.Destroy());
            _levelBDEs.Clear();
        }

        private void SetupForCurrentExpedition()
        {
            foreach(var zoneBDE in GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                if(_levelBDEs.ContainsKey(zoneBDE.IntTuple))
                {
                    EOSLogger.Warning($"BossDeathEvent: found duplicate setup for zone {zoneBDE}, will overwrite!");
                }

                if(zoneBDE.ApplyToHibernateCount != UNLIMITED_COUNT || zoneBDE.ApplyToWaveCount != UNLIMITED_COUNT)
                {
                    uint alloted_id = EOSNetworking.AllotReplicatorID();
                    if(alloted_id != EOSNetworking.INVALID_ID)
                    {
                        zoneBDE.SetupReplicator(alloted_id);
                    }
                    else
                    {
                        EOSLogger.Error($"BossDeathEvent: replicator ID depleted, cannot setup replicator!");
                    }
                }

                _levelBDEs[zoneBDE.IntTuple] = zoneBDE;
            }
        }

        public bool TryConsumeBDEventsExecutionTimes(EventsOnZoneBossDeath def, Mode mode)
        {
            if (!_levelBDEs.TryGetValue(def.IntTuple, out var bde))
            {
                EOSLogger.Error($"BossDeathEventManager: got an unregistered entry: {def} {mode}");
                return false;
            }

            int remain = mode == Mode.HIBERNATE ? bde.HibernateCount : bde.WaveCount;
            if (remain == UNLIMITED_COUNT)
            {
                return true;
            }
            else if (remain > 0)
            {
                bde.Replicator?.SetStateUnsynced(new()
                {
                    applyToHibernateCount = mode == Mode.HIBERNATE ? remain - 1 : bde.HibernateCount,
                    applyToWaveCount = mode == Mode.WAVE ? remain - 1 : bde.WaveCount
                });
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
