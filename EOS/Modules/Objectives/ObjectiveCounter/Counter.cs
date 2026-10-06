using AmorLib.Networking.StateReplicators;

namespace EOS.Modules.Objectives.ObjectiveCounter
{
    public unsafe struct CounterStatus
    {
        public int count;

        public fixed ulong executeOnce[16];

        public CounterStatus(int count, ulong[] source)
        {
            this.count = count;
            fixed (ulong* dest = executeOnce)
            {
                source.AsSpan(0, 16).CopyTo(new Span<ulong>(dest, 16));
            }
        }
    }

    public class Counter
    {
        public ObjectiveCounterDefinition Def { get; private set; }
        public int CurrentCount { get; private set; } = 0;
        public ulong[] ExecuteOnce { get; private set; } = new ulong[16];
        public List<int> ExecuteOnceOverflow { get; private set; } = new();
        public StateReplicator<CounterStatus>? Replicator { get; private set; }

        private int Clamp(long count) => (int)Math.Clamp(count, Def.MinCount, Def.MaxCount);

        public Counter(ObjectiveCounterDefinition def)
        {
            Def = def;
            CurrentCount = def.StartingCount;

            uint allottedID = EOSNetworking.AllotReplicatorID();
            if (allottedID == EOSNetworking.INVALID_ID)
            {
                EOSLogger.Error("Counter: replicator IDs depleted, cannot setup StateReplicator");
                return;
            }

            Replicator = StateReplicator<CounterStatus>.Create(allottedID, new() { count = Def.StartingCount }, LifeTimeType.Session);
            Replicator!.OnStateChanged += OnStateChanged;
        }

        private void OnStateChanged(CounterStatus _, CounterStatus state, bool isRecall)
        {
            CurrentCount = state.count;
            unsafe
            {
                ulong* src = state.executeOnce;
                for (int i = 0; i < ExecuteOnce.Length; i++)
                {
                    ExecuteOnce[i] = src[i];
                }
            }
        }

        private void ReachTo(int count)
        {
            EOSLogger.Debug($"Counter '{Def.WorldEventObjectFilter}' reached {count}");
            for (int idx = 0; idx < Def.OnReached.Count; idx++)
            {
                var counter = Def.OnReached[idx];
                if (counter.Count != count || (counter.ExecuteOnce && HasExecuted(idx))) 
                    continue;

                EOSWardenEventManager.ExecuteWardenEvents(counter.EventsOnReached);
            }
        }

        private unsafe bool HasExecuted(int index)
        {
            if (index >= 1024)
            {
                if (ExecuteOnceOverflow.Contains(index))
                {
                    EOSLogger.Warning($"Counter '{Def.WorldEventObjectFilter}' ExecuteOnce index {index} > 1024, will not sync on recall");
                    return true;
                }
                else
                {
                    ExecuteOnceOverflow.Add(index);
                    return false;
                }
            }

            int word = index >> 6;
            int bit = index & 63;
            bool result = false;

            fixed (ulong* mask = ExecuteOnce)
            {
                result = (mask[word] & (1UL << bit)) != 0;
                if (!result) mask[word] |= 1UL << bit;
            }
            return result;
        }

        public void Increment(int by)
        {
            long prev = CurrentCount;
            CurrentCount = Clamp(prev + by);
            for (long num = prev + 1; num <= CurrentCount; num++)
            {
                ReachTo((int)num);
            }
            Replicator?.SetStateUnsynced(new(CurrentCount, ExecuteOnce));
        }

        public void Decrement(int by)
        {
            long prev = CurrentCount;
            CurrentCount = Clamp(prev - by);
            for (long num = prev - 1; num >= CurrentCount; num--)
            {
                ReachTo((int)num);
            }
            Replicator?.SetStateUnsynced(new(CurrentCount, ExecuteOnce));
        }

        public void Set(int num)
        {
            CurrentCount = Clamp(num);
            ReachTo(CurrentCount);
            Replicator?.SetStateUnsynced(new(CurrentCount, ExecuteOnce));
        }

        public void Jump(int num)
        {
            long prev = CurrentCount;
            CurrentCount = Clamp(prev + num);
            ReachTo(CurrentCount);
            Replicator?.SetStateUnsynced(new(CurrentCount, ExecuteOnce));
        }
    }
}
