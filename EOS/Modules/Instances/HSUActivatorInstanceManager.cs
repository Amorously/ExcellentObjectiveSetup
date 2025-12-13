using AmorLib.Utils;
using EOS.BaseClasses;
using LevelGeneration;

namespace EOS.Modules.Instances
{
    public sealed class HSUActivatorInstanceManager: InstanceManager<LG_HSUActivator_Core>
    {
        public static HSUActivatorInstanceManager Current { get; private set; } = new();

        public override (int, int, int) GetGlobalIndex(LG_HSUActivator_Core instance) => instance.SpawnNode.m_zone.ToIntTuple();
    }
}
