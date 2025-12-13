using EOS.BaseClasses;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Objectives.IndividualGenerator
{
    internal sealed class IndividualGeneratorObjectiveManager : InstanceDefinitionManager<IndividualGeneratorDefinition>
    {
        protected override string DEFINITION_NAME { get; } = "IndividualGenerator";
        
        public static IndividualGeneratorObjectiveManager Current { private set; get; } = new();

        public bool TryGetDefinition(LG_PowerGenerator_Core instance, [MaybeNullWhen(false)] out IndividualGeneratorDefinition definition)
        {
            var (globalIndex, instanceIndex) = Modules.Instances.PowerGeneratorInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }
    }
}
