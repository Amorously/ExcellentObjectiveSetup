using AmorLib.Utils;
using EOS.BaseClasses;

namespace EOS.Modules.Instances
{
    public sealed class GeneratorClusterInstanceManager: InstanceManager<LG_PowerGeneratorCluster>
    {
        public static GeneratorClusterInstanceManager Current { get; private set; } = new();

        public override (int, int, int) GetGlobalIndex(LG_PowerGeneratorCluster instance) => instance.SpawnNode.m_zone.ToIntTuple();
    }
}
