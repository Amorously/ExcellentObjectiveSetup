using AmorLib.Networking.StateReplicators;

namespace EOS.Modules.Objectives.ObjectiveCounter
{
    public struct CounterStatus
    {
        public int count;
    }

    public class Counter
    {
        public ObjectiveCounterDefinition Def { get; private set; }
        public int CurrentCount { get; private set; } = 0;        
        public string WorldEventObjectFilter => Def.WorldEventObjectFilter;
        public StateReplicator<CounterStatus>? StateReplicator { get; private set; }

        private readonly HashSet<OnCounter> _executedOnce = new(); 
        
        public Counter(ObjectiveCounterDefinition def)
        {
            Def = def;
            CurrentCount = def.StartingCount;
        }

        internal bool TrySetupReplicator()
        {
            uint id = EOSNetworking.AllotReplicatorID();
            if (id == EOSNetworking.INVALID_ID) return false;

            StateReplicator = StateReplicator<CounterStatus>.Create(id, new() { count = Def.StartingCount }, LifeTimeType.Session)!;
            StateReplicator.OnStateChanged += OnStateChanged;
            return true;
        }

        private void OnStateChanged(CounterStatus _, CounterStatus state, bool isRecall)
        {
            if (state.count != CurrentCount)
            {
                CurrentCount = state.count;
            }
        }

        private void ReachTo(int count)
        {
            var counters = Def.OnReached.Where(c => c.Count == count);
            foreach (var counter in counters)
            {
                if (counter.ExecuteOnce && _executedOnce.Contains(counter)) continue;
                EOSWardenEventManager.ExecuteWardenEvents(counter.EventsOnReached);

                if (counter.ExecuteOnce)
                    _executedOnce.Add(counter);
            }
            EOSLogger.Debug($"Counter '{WorldEventObjectFilter}' reached {count}");
        }

        public void Increment(int by)
        {
            int prev = CurrentCount;
            CurrentCount += by;

            for (int num = prev + 1; num <= CurrentCount; num++)
                ReachTo(num);

            StateReplicator?.SetStateUnsynced(new() { count = CurrentCount });
        }

        public void Decrement(int by)
        {
            int prev = CurrentCount;
            CurrentCount -= by;

            for (int num = prev - 1; num >= CurrentCount; num--)
                ReachTo(num);

            StateReplicator?.SetStateUnsynced(new() { count = CurrentCount });
        }

        public void Set(int num)
        {
            CurrentCount = num;
            ReachTo(num);
            StateReplicator?.SetStateUnsynced(new() { count = CurrentCount });
        }
    }
}
