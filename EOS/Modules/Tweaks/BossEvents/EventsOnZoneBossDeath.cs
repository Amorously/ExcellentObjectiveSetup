using AmorLib.Networking.StateReplicators;
using EOS.BaseClasses;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Tweaks.BossEvents
{
    public struct FiniteBDEState 
    {
        public int ApplyToHibernateCount = int.MaxValue;

        public int ApplyToWaveCount = int.MaxValue;

        public FiniteBDEState() { }

        public FiniteBDEState(FiniteBDEState other)
        {
            ApplyToHibernateCount = other.ApplyToHibernateCount;
            ApplyToHibernateCount = other.ApplyToWaveCount;
        }

        public FiniteBDEState(int hibernateCount, int waveCount)
        {
            ApplyToHibernateCount = hibernateCount;
            ApplyToWaveCount = waveCount;
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
        public StateReplicator<FiniteBDEState> FiniteBDEStateReplicator { get; private set; } = null!;
    
        public void SetupReplicator(uint replicatorID)
        {
            if (ApplyToHibernateCount == BossDeathEventManager.UNLIMITED_COUNT && ApplyToWaveCount == BossDeathEventManager.UNLIMITED_COUNT)
            {
                return; // state replicator is not required
            }

            FiniteBDEStateReplicator = StateReplicator<FiniteBDEState>.Create(replicatorID, new() { ApplyToHibernateCount = ApplyToHibernateCount, ApplyToWaveCount = ApplyToWaveCount }, LifeTimeType.Session)!;
            FiniteBDEStateReplicator.OnStateChanged += OnStateChanged;
        }

        private void OnStateChanged(FiniteBDEState oldState, FiniteBDEState newState, bool isRecall)
        {
            // for now i dont know what to do here
        }

        internal void Destroy()
        {
            FiniteBDEStateReplicator = null!;
        }

        [JsonIgnore]
        public int RemainingWaveBDE => FiniteBDEStateReplicator != null ? FiniteBDEStateReplicator.State.ApplyToWaveCount : ApplyToWaveCount;

        [JsonIgnore]
        public int RemainingHibernateBDE => FiniteBDEStateReplicator != null ? FiniteBDEStateReplicator.State.ApplyToHibernateCount : ApplyToHibernateCount;
    }
}
