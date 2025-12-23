using EOS.Modules.Expedition.Gears;
using EOS.Modules.Expedition.IndividualGeneratorGroup;

namespace EOS.Modules.Expedition
{
    public class ExpeditionDefinition // Add expedition definition as needed
    {
        public uint MainLevelLayout { set; get; } = 0u;     
        
        public ExpeditionGearsDefinition ExpeditionGears { set; get; } = new();

        public List<ExpeditionIGGroup> GeneratorGroups { set; get; } = new() { new() };
    }
}
