using EOS.BaseClasses;
using GameData;

namespace EOS.Modules.Tweaks.Scout
{
    public class EventsOnZoneScoutScream: GlobalBased
    {
        public bool SuppressVanillaScoutWave { get; set; } = false;
        public List<WardenObjectiveEventData> EventsOnScoutScream { get; set; } = new();
    }
}
