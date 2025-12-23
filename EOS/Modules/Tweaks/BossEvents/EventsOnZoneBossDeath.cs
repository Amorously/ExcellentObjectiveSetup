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
        public int HibernateCount => Replicator?.State.applyToHibernateCount ?? int.MaxValue;

        [JsonIgnore]
        public int WaveCount => Replicator?.State.applyToWaveCount ?? int.MaxValue;

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
        }

        internal void Destroy()
        {
            Replicator?.Unload();
            Replicator = null;
        }
    }
}
