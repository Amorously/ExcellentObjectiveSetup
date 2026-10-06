using EOS.Modules.Objectives.ObjectiveCounter;
using GTFO.API;
using HarmonyLib;

namespace EOS.Patches
{
    [HarmonyPatch]
    internal static class Patch_PLOC_Downed_CommonEnter
    {
        private static readonly List<string> _downedCounters = new();
        private static bool _initialized = false;

        static Patch_PLOC_Downed_CommonEnter()
        {
            LevelAPI.OnLevelCleanup += () =>
            {
                _downedCounters.Clear();
                _initialized = false;
            };
        }

        [HarmonyPatch(typeof(PLOC_Downed), nameof(PLOC_Downed.CommonEnter))]
        [HarmonyPostfix]
        private static void PlayerDownedCounter()
        {
            if (!_initialized)
            {
                foreach (var counter in ObjectiveCounterManager.Current.Counters.Values)
                {
                    if (counter.Def.IncrementOnPlayerDowned)
                        _downedCounters.Add(counter.Def.WorldEventObjectFilter);
                }
                _initialized = true;
            }

            foreach (var key in _downedCounters)
            { 
                if (ObjectiveCounterManager.Current.Counters.TryGetValue(key, out var counter))                
                    counter.Increment(1);                
            }            
        }
    }
}
