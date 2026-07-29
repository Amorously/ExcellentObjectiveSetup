using EOS.BaseClasses.CustomTerminalDefinition;
using EOS.Modules.Expedition.Gears;
using EOS.Modules.Expedition.IndividualGeneratorGroup;
using GameData;

namespace EOS.Modules.Expedition
{
    public class ExpeditionDefinition 
    {
        public uint MainLevelLayout { get; set; } = 0u;     
        
        public ExpeditionGearsDefinition ExpeditionGears { get; set; } = new();

        public List<ExpeditionIGGroup> GeneratorGroups { get; set; } = new() { new() };

        public List<ExpeditionTerminalsDefinition> Terminals { get; set; } = new() { new() };
    }

    public class ExpeditionTerminalsDefinition : BaseTerminalDefinition
    {
        public List<WardenObjectiveEventData> EventsOnApproach { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnPasswordInputSuccess {  get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnPasswordInputFailure { get; set; } = new();

        public List<TerminalLogFileEvents> LogFiles { get; set; } = new();
    }

    public class TerminalLogFileEvents
    {
        public string FileName { get; set; } = string.Empty;

        public List<WardenObjectiveEventData> EventsOnFileRead { get; set; } = new();
    }
}
