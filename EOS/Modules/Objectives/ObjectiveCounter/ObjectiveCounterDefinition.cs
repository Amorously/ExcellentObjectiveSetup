using GameData;

namespace EOS.Modules.Objectives.ObjectiveCounter
{
    public class OnCounter 
    {
        public int Count { get; set; } = -1;

        public bool ExecuteOnce { get; set; } = false;

        public List<WardenObjectiveEventData> EventsOnReached { get; set; } = new();
    }

    public class ObjectiveCounterDefinition
    {
        public string WorldEventObjectFilter { get; set; } = string.Empty;

        public int StartingCount { get; set; } = 0;

        public List<OnCounter> OnReached { get; set; } = new() { new() };
    }
}
