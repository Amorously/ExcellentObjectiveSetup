using AmorLib.Networking.StateReplicators;
using EOS.BaseClasses;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Tweaks.BossEvents
{
    public struct FiniteBDEState 
    {
        public int applyToHibernateCount = int.MaxValue;

        public int applyToWaveCount = int.MaxValue;

        public FiniteBDEState() { }

        public FiniteBDEState(FiniteBDEState other)
        {
            applyToHibernateCount = other.applyToHibernateCount;
            applyToHibernateCount = other.applyToWaveCount;
        }

        public FiniteBDEState(int hibernateCount, int waveCount)
        {
            applyToHibernateCount = hibernateCount;
            applyToWaveCount = waveCount;
        }
    }

    public class EventsOnZoneBossDeath: GlobalBased
    {
        public bool ApplyToHibernate { get; set; } = true;     
        
        public int ApplyToHibernateCount { get; set; } = BossDeathEventManager.UNLIMITED_COUNT;     
        
        public bool ApplyToWave { get; set; } = false;

        public int ApplyToWaveCount { get; set; } = BossDeathEventManager.UNLIMITED_COUNT;

        public List<uint> BossIDs { set; get; } = new() { 29, 36, 37 };

        public List<WardenObjectiveEventData> EventsOnBossDeath { set; get; } = new();

        [JsonIgnore]
        public StateReplicator<FiniteBDEState>? FiniteBDEStateReplicator { get; private set; }
        
        [JsonIgnore]
        public int HibernateCount { get; private set; } = int.MaxValue;

        [JsonIgnore]
        public int WaveCount { get; private set; } = int.MaxValue;

        public void SetupReplicator(uint replicatorID)
        {
            if (ApplyToHibernateCount == BossDeathEventManager.UNLIMITED_COUNT && ApplyToWaveCount == BossDeathEventManager.UNLIMITED_COUNT)
            {
                return; // state replicator is not required
            }

            FiniteBDEStateReplicator = StateReplicator<FiniteBDEState>.Create(replicatorID, new() { applyToHibernateCount = ApplyToHibernateCount, applyToWaveCount = ApplyToWaveCount }, LifeTimeType.Session)!;
            FiniteBDEStateReplicator.OnStateChanged += OnStateChanged;
        }

        private void OnStateChanged(FiniteBDEState _, FiniteBDEState state, bool isRecall)
        {
            if (state.applyToHibernateCount != ApplyToHibernateCount)
                HibernateCount = state.applyToHibernateCount;

            if (state.applyToWaveCount != ApplyToWaveCount)
                WaveCount = state.applyToWaveCount;
        }

        internal void Destroy()
        {
            FiniteBDEStateReplicator?.Unload();
            FiniteBDEStateReplicator = null;
        }

        [JsonIgnore]
        public int RemainingWaveBDE => FiniteBDEStateReplicator != null ? WaveCount : ApplyToWaveCount;

        [JsonIgnore]
        public int RemainingHibernateBDE => FiniteBDEStateReplicator != null ? HibernateCount : ApplyToHibernateCount;
    }
}
