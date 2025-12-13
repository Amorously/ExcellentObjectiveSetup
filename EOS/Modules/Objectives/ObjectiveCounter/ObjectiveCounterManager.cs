using EOS.BaseClasses;
using GameData;

namespace EOS.Modules.Objectives.ObjectiveCounter
{
    public sealed class ObjectiveCounterManager : GenericExpeditionDefinitionManager<ObjectiveCounterDefinition>
    {
        public enum CounterWardenEvent
        {
            ChangeCounter = 500,
            SetCounter = 501
        }

        protected override string DEFINITION_NAME => "ObjectiveCounter";
        
        public static ObjectiveCounterManager Current { get; } = new();

        private Dictionary<string, Counter> _counters { get; } = new();        

        public ObjectiveCounterManager() 
        {
            EOSWardenEventManager.AddEventDefinition(CounterWardenEvent.ChangeCounter.ToString(), (uint)CounterWardenEvent.ChangeCounter, ChangeCounter);
            EOSWardenEventManager.AddEventDefinition(CounterWardenEvent.SetCounter.ToString(), (uint)CounterWardenEvent.SetCounter, SetCounter);
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnBuildDone() // BuildCounters
        {
            if (!Definitions.ContainsKey(CurrentMainLevelLayout)) return;
            Definitions[CurrentMainLevelLayout].Definitions.ForEach(Build);
        }

        protected override void OnLevelCleanup()
        {
            _counters.Clear();
        }
        
        private void Build(ObjectiveCounterDefinition def)
        {
            if(_counters.ContainsKey(def.WorldEventObjectFilter))
            {
                EOSLogger.Error($"Build Counter: counter '{def.WorldEventObjectFilter}' already exists...");
                return;
            }

            var counter = new Counter(def);
            if(!counter.TrySetupReplicator())
            {
                EOSLogger.Error($"Build Counter: counter '{def.WorldEventObjectFilter}' failed to setup state replicator! What's going wrong?");
                return;
            }

            _counters[def.WorldEventObjectFilter] = counter;
            EOSLogger.Debug($"Build Counter: counter '{def.WorldEventObjectFilter}' setup completed");
        }

        private void ChangeCounter(WardenObjectiveEventData e)
        {
            if (!_counters.TryGetValue(e.WorldEventObjectFilter, out var counter))
            {
                EOSLogger.Error($"ChangeCounter: {e.WorldEventObjectFilter} is not defined");
                return;
            }
            
            int by = e.Count;
            if (by > 0)
            {
                counter.Increment(by);
            }
            else if (by < 0)
            {
                counter.Decrement(by);
            }

        }

        private void SetCounter(WardenObjectiveEventData e)
        {
            if (!_counters.TryGetValue(e.WorldEventObjectFilter, out var counter))
            {
                EOSLogger.Error($"ChangeCounter: {e.WorldEventObjectFilter} is not defined");
                return;
            }
            counter.Set(e.Count);
        }
    }
}
