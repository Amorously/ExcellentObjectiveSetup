using EOS.BaseClasses;

namespace EOS.Modules.Tweaks.ScoutEvents
{
    internal sealed class ScoutScreamEventManager: ZoneDefinitionManager<EventsOnZoneScoutScream, ScoutScreamEventManager>
    {
        protected override string DEFINITION_NAME => "EventsOnScoutScream";
    }
}
