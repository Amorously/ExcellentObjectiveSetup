using AmorLib.Utils.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace EOS.BaseClasses
{
    public abstract class InstanceManager<T, TBase> : BaseManager<TBase>
        where T : Il2CppSystem.Object
        where TBase : InstanceManager<T, TBase>
    {
        public const uint INVALID_INSTANCE_INDEX = uint.MaxValue;

        protected override string DEFINITION_NAME => string.Empty;

        protected Dictionary<(int, int, int), Dictionary<IntPtr, uint>> Instances2Index { get; } = new();
        protected Dictionary<(int, int, int), List<T>> Index2Instance { get; } = new();

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            Instances2Index.Clear();
            Index2Instance.Clear();
        }
        
        public virtual uint Register(T instance) => Register(GetGlobalIndex(instance), instance);

        public virtual uint Register((int, int, int) globalIndex, T instance)
        {
            if (instance == null) return INVALID_INSTANCE_INDEX;

            var instancesInZone = Instances2Index.GetOrAddNew(globalIndex);
            if (instancesInZone.ContainsKey(instance.Pointer))
            {
                EOSLogger.Warning($"InstanceManager<{typeof(T)}>: trying to register duplicate instance! Skipped....");
                return INVALID_INSTANCE_INDEX;
            }

            uint instanceIndex = (uint)instancesInZone.Count; // starts from 0
            instancesInZone[instance.Pointer] = instanceIndex;
            Index2Instance.GetOrAddNew(globalIndex).Add(instance);

            return instanceIndex;
        }

        public abstract (int, int, int) GetGlobalIndex(T instance);

        public uint GetInstanceIndex(T instance, (int, int, int)? globalIndex = null)
        {
            globalIndex ??= GetGlobalIndex(instance);

            if (!Instances2Index.TryGetValue(globalIndex.Value, out var zoneMap))
                return INVALID_INSTANCE_INDEX;
            
            return zoneMap.TryGetValue(instance.Pointer, out var index) ? index : INVALID_INSTANCE_INDEX;
        }

        public ((int, int, int), uint) GetGlobalInstance(T instance)
        {
            var globalIndex = GetGlobalIndex(instance);
            var instanceIndex = GetInstanceIndex(instance, globalIndex);
            return (globalIndex, instanceIndex);
        }
                
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

        public IReadOnlyList<T> GetInstancesInZone((int, int, int) globalIndex)
        { 
            return TryGetInstancesInZone(globalIndex, out var instances) ? instances : new List<T>();
        }

        public bool TryGetInstancesInZone((int, int, int) globalIndex, out IReadOnlyList<T> instances)
        {
            if (Index2Instance.TryGetValue(globalIndex, out var list))
            {
                instances = list;
                return true;
            }
            instances = new List<T>();
            return false;
        }

        public bool IsRegistered(T instance)
        {
            return Instances2Index.TryGetValue(GetGlobalIndex(instance), out var map) && map.ContainsKey(instance.Pointer);
        }

        public IEnumerable<(int, int, int)> RegisteredZones() => Index2Instance.Keys;
    }
}
