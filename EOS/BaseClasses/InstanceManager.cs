using AmorLib.Utils.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class InstanceManager<T> : BaseManager where T : Il2CppSystem.Object
    {
        public const uint INVALID_INSTANCE_INDEX = uint.MaxValue;

        public Type InstanceType => typeof(T);

        protected override string DEFINITION_NAME => string.Empty;

        protected Dictionary<(int dim, int layer, int zone), Dictionary<IntPtr, uint>> Instances2Index { get; } = new();
        protected Dictionary<(int dim, int layer, int zone), List<T>> Index2Instance { get; } = new();

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            Instances2Index.Clear();
            Index2Instance.Clear();
        }
        
        public virtual uint Register(T instance) => Register(GetGlobalIndex(instance), instance);

        public abstract (int dim, int layer, int zone) GetGlobalIndex(T instance);

        public virtual uint Register((int dim, int layer, int zone) globalIndex, T instance)
        {
            if (instance == null) return INVALID_INSTANCE_INDEX;

            var instancesInZone = Instances2Index.GetOrAddNew(globalIndex);
            if (instancesInZone.ContainsKey(instance.Pointer))
            {
                EOSLogger.Error($"InstanceManager<{typeof(T)}>: trying to register duplicate instance! Skipped....");
                return INVALID_INSTANCE_INDEX;
            }

            uint instanceIndex = (uint)instancesInZone.Count; // starts from 0
            instancesInZone[instance.Pointer] = instanceIndex;
            Index2Instance.GetOrAddNew(globalIndex).Add(instance);

            return instanceIndex;
        }        

        public uint GetZoneInstanceIndex(T instance)
        {
            var zone = GetGlobalIndex(instance);

            if (!Instances2Index.TryGetValue(zone, out var zoneMap))
                return INVALID_INSTANCE_INDEX;

            return zoneMap.TryGetValue(instance.Pointer, out var index) ? index : INVALID_INSTANCE_INDEX;
        }

        public ((int, int, int), uint) GetGlobalInstance(T instance)
        {
            return (GetGlobalIndex(instance), GetZoneInstanceIndex(instance)); 
        }
                
        public T? GetInstance(int dim, int layer, int zone, uint instanceIndex) => GetInstance((dim, layer, zone), instanceIndex);

        public T? GetInstance((int, int, int) globalIndex, uint instanceIndex)
        {
            return TryGetInstance(globalIndex, instanceIndex, out var instance) ? instance : null;
        }

        public bool TryGetInstance((int, int, int) globalIndex, uint instanceIndex, [MaybeNullWhen(false)] out T instance)
        {
            instance = null;

            if (!Index2Instance.TryGetValue(globalIndex, out var instances) || instanceIndex >= instances.Count)
                return false;

            instance = instances[(int)instanceIndex];
            return true;
        }


        public IReadOnlyList<T> GetInstancesInZone(int dim, int layer, int zone) => GetInstancesInZone((dim, layer, zone));

        public IReadOnlyList<T> GetInstancesInZone((int, int, int) globalIndex)
        { 
            return TryGetInstancesInZone(globalIndex, out var instances) ? instances : new List<T>();
        }

        public bool TryGetInstancesInZone((int, int, int) globalZoneIndex, out IReadOnlyList<T> instances)
        {
            if (Index2Instance.TryGetValue(globalZoneIndex, out var list))
            {
                instances = list;
                return true;
            }
            instances = new List<T>();
            return false;
        }

        public bool IsRegistered(T instance)
        {
            var zone = GetGlobalIndex(instance);
            return Instances2Index.TryGetValue(zone, out var map) && map.ContainsKey(instance.Pointer);
        }

        public IEnumerable<(int, int, int)> RegisteredZones() => Index2Instance.Keys;
    }
}
