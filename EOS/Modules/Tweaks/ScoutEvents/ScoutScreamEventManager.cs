using EOS.BaseClasses;

namespace EOS.Modules.Tweaks.ScoutEvents
{
    public sealed class ScoutScreamEventManager: ZoneDefinitionManager<EventsOnZoneScoutScream, ScoutScreamEventManager>
    {
        protected override string DEFINITION_NAME => "EventsOnScoutScream";
    }
}
