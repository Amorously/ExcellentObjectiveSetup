using AmorLib.Networking.StateReplicators;
using EOS.BaseClasses;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Tweaks.BossEvents
{
    public class EventsOnZoneBossDeath: GlobalBased
    {
        public bool ApplyToHibernate { get; set; } = true;     
        
        public int ApplyToHibernateCount { get; set; } = BossDeathEventManager.UNLIMITED_COUNT;     
        
        public bool ApplyToWave { get; set; } = false;

        public int ApplyToWaveCount { get; set; } = BossDeathEventManager.UNLIMITED_COUNT;

        public List<uint> BossIDs { set; get; } = new() { 29, 36, 37 };

        public List<WardenObjectiveEventData> EventsOnBossDeath { set; get; } = new();

        [JsonIgnore]
        public StateReplicator<FiniteBDEState>? Replicator { get; private set; }
        
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

            Replicator = StateReplicator<FiniteBDEState>.Create(replicatorID, new() 
            { 
                applyToHibernateCount = ApplyToHibernateCount, 
                applyToWaveCount = ApplyToWaveCount 
            }, LifeTimeType.Session);

            Replicator!.OnStateChanged += OnStateChanged;
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
            Replicator?.Unload();
            Replicator = null;
        }
    }
}
