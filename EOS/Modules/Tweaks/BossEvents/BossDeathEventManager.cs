using EOS.BaseClasses;
using GameData;
using LevelGeneration;
using SNetwork;
using System.Collections.Concurrent;

namespace EOS.Modules.Tweaks.BossEvents
{
    internal sealed class BossDeathEventManager : ZoneDefinitionManager<EventsOnZoneBossDeath>
    {
        public static BossDeathEventManager Current = new();

        public enum Mode { HIBERNATE, WAVE }

        public const int UNLIMITED_COUNT = int.MaxValue;

        // maintained on both host and client side.
        private ConcurrentDictionary<(eDimensionIndex, LG_LayerType, eLocalZoneIndex), EventsOnZoneBossDeath> LevelBDEs { get; } = new();

        protected override string DEFINITION_NAME => "EventsOnBossDeath";

        public bool TryConsumeBDEventsExecutionTimes(EventsOnZoneBossDeath def, Mode mode) => TryConsumeBDEventsExecutionTimes(def.DimensionIndex, def.LayerType, def.LocalIndex, mode);

        public bool TryConsumeBDEventsExecutionTimes(eDimensionIndex dimensionIndex, LG_LayerType layer, eLocalZoneIndex localIndex, Mode mode)
        {
            if(!LevelBDEs.ContainsKey((dimensionIndex, layer, localIndex)))
            {
                EOSLogger.Error($"BossDeathEventManager: got an unregistered entry: {(dimensionIndex, layer, localIndex, mode)}");
                return false;
            }

            var bde = LevelBDEs[(dimensionIndex, layer, localIndex)];
            int remain = mode == Mode.HIBERNATE ? bde.RemainingHibernateBDE : bde.RemainingWaveBDE;
            
            if (remain == UNLIMITED_COUNT)
            {
                return true;
            }
            else if (remain > 0)
            {
                var oldState = bde.FiniteBDEStateReplicator.State;
                 
                if(SNet.IsMaster)
                {
                    bde.FiniteBDEStateReplicator.SetState(new()
                    {
                        ApplyToHibernateCount = mode == Mode.HIBERNATE ? remain - 1 : oldState.ApplyToHibernateCount,
                        ApplyToWaveCount = mode == Mode.WAVE ? remain - 1 : oldState.ApplyToWaveCount,
                    });
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnBuildStart()
        {
            OnLevelCleanup();
            SetupForCurrentExpedition();
        }

        protected override void OnLevelCleanup()
        {
            foreach(var bde in LevelBDEs.Values)
            {
                bde.Destroy();
            }

            LevelBDEs.Clear();
        }

        private void SetupForCurrentExpedition()
        {
            if (!Definitions.ContainsKey(CurrentMainLevelLayout)) return;

            foreach(var zoneBDE in Definitions[CurrentMainLevelLayout].Definitions)
            {
                if(LevelBDEs.ContainsKey(zoneBDE.GlobalZoneIndexTuple()))
                {
                    EOSLogger.Error($"BossDeathEvent: found duplicate setup for zone {zoneBDE.GlobalZoneIndexTuple()}, will overwrite!");
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

                LevelBDEs[zoneBDE.GlobalZoneIndexTuple()] = zoneBDE;
            }
        }
    }
}
