using AmorLib.Utils;
using EOS.BaseClasses;
using EOS.Modules.Objectives.IndividualGenerator;
using LevelGeneration;
using System.Text;

namespace EOS.Modules.Instances
{
    public sealed class PowerGeneratorInstanceManager: InstanceManager<LG_PowerGenerator_Core, PowerGeneratorInstanceManager>
    {
        private readonly Dictionary<IntPtr, LG_PowerGeneratorCluster> _gcGenerators = new();
        
        //protected override void OnBuildDone() // OutputLevelInstanceInfo
        //{
        //    StringBuilder sb = new();

        //    foreach (var globalIndex in RegisteredZones())
        //    {
        //        var PGInstanceInZone = GetInstancesInZone(globalIndex);
        //        for (int instanceIndex = 0; instanceIndex < PGInstanceInZone.Count; instanceIndex++)
        //        {
        //            var PGInstance = PGInstanceInZone[instanceIndex];
        //            sb.AppendLine($"GENERATOR_{PGInstance.m_serialNumber}. Global index: (D{globalIndex.Item1}, L{globalIndex.Item2}, Z{globalIndex.Item3}), Instance index: {instanceIndex}");
        //        }
        //        sb.AppendLine();
        //    }

        //    string msg = sb.ToString();
        //    if (!string.IsNullOrWhiteSpace(msg))
        //        EOSLogger.Debug(msg);
        //}
        
        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _gcGenerators.Clear();
            base.OnLevelCleanup();
        }

        public override (int, int, int) GetGlobalIndex(LG_PowerGenerator_Core instance) => instance.SpawnNode.m_zone.ToIntTuple();

        public override uint Register((int, int, int) globalZoneIndex, LG_PowerGenerator_Core instance) 
        { 
            if(_gcGenerators.ContainsKey(instance.Pointer))
            {
                EOSLogger.Error("PowerGeneratorInstanceManager: Trying to register a GC Generator, which is an invalid operation");
                return INVALID_INSTANCE_INDEX;
            }

            return base.Register(globalZoneIndex, instance);
        }

        public void MarkAsGCGenerator(LG_PowerGeneratorCluster parent, LG_PowerGenerator_Core child)
        {
            if(IsRegistered(child))
            {
                EOSLogger.Error("PowerGeneratorInstanceManager: Trying to mark a registered Generator as GC Generator, which is an invalid operation");
                return;
            }

            _gcGenerators[child.Pointer] = parent;
        }

        public bool IsGCGenerator(LG_PowerGenerator_Core instance) => _gcGenerators.ContainsKey(instance.Pointer);

        public LG_PowerGeneratorCluster? GetParentGeneratorCluster(LG_PowerGenerator_Core instance) =>_gcGenerators.TryGetValue(instance.Pointer, out var gc) ? gc : null;
    }
}
