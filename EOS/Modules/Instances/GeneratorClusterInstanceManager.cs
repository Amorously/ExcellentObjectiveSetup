using AmorLib.Utils;
using EOS.BaseClasses;

namespace EOS.Modules.Instances
{
    public sealed class GeneratorClusterInstanceManager: InstanceManager<LG_PowerGeneratorCluster, GeneratorClusterInstanceManager>
    {
        public override (int, int, int) GetGlobalIndex(LG_PowerGeneratorCluster instance) => instance.SpawnNode.m_zone.ToIntTuple();
    }
}
