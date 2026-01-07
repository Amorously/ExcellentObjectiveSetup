using AmorLib.Utils.Extensions;
using EOS.BaseClasses;
using GameData;

namespace EOS.Modules.Objectives.ObjectiveCounter
{
    public sealed class ObjectiveCounterManager : GenericExpeditionDefinitionManager<ObjectiveCounterDefinition, ObjectiveCounterManager>
    {
        public enum CounterWardenEvent
        {
            ChangeCounter = 500,
            SetCounter = 501
        }

        protected override string DEFINITION_NAME => "ObjectiveCounter";
        
        public IReadOnlyDictionary<string, Counter> Counters => _counters;
        private readonly Dictionary<string, Counter> _counters = new();

        static ObjectiveCounterManager() 
        {
            EOSWardenEventManager.AddEventDefinition(CounterWardenEvent.ChangeCounter.ToString(), (uint)CounterWardenEvent.ChangeCounter, ChangeCounter);
            EOSWardenEventManager.AddEventDefinition(CounterWardenEvent.SetCounter.ToString(), (uint)CounterWardenEvent.SetCounter, SetCounter);
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnBuildDone() // BuildCounters
        {
            if (!GenericExpDefinitions.ContainsKey(CurrentMainLevelLayout)) return;
            GenericExpDefinitions[CurrentMainLevelLayout].Definitions.ForEach(Build);
        }

        protected override void OnLevelCleanup()
        {
            _counters.ForEachValue(c => c.Replicator?.Unload());
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
            _counters[def.WorldEventObjectFilter] = counter;
            EOSLogger.Debug($"Build Counter: counter '{def.WorldEventObjectFilter}' setup completed");
        }

        private static void ChangeCounter(WardenObjectiveEventData e)
        {
            if (!Current._counters.TryGetValue(e.WorldEventObjectFilter, out var counter))
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
                counter.Decrement(Math.Abs(by));
            }
        }

        private static void SetCounter(WardenObjectiveEventData e)
        {
            if (!Current._counters.TryGetValue(e.WorldEventObjectFilter, out var counter))
            {
                EOSLogger.Error($"ChangeCounter: {e.WorldEventObjectFilter} is not defined");
                return;
            }
            counter.Set(e.Count);
        }
    }
}
