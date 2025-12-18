using EOS.BaseClasses;

namespace EOS.Modules.Tweaks.Scout
{
    internal sealed class ScoutScreamEventManager: ZoneDefinitionManager<EventsOnZoneScoutScream, ScoutScreamEventManager>
    {
        protected override string DEFINITION_NAME => "EventsOnScoutScream";
    }
}
