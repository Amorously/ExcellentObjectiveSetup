using EOS.BaseClasses;

namespace EOS.Modules.Tweaks.Scout
{
    internal sealed class ScoutScreamEventManager: ZoneDefinitionManager<EventsOnZoneScoutScream>
    {
        public static ScoutScreamEventManager Current = new();

        protected override string DEFINITION_NAME => "EventsOnScoutScream";
    }
}
