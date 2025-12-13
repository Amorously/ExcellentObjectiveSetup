using AmorLib.Utils;
using ChainedPuzzles;
using EOS.BaseClasses;
using pCPState = ChainedPuzzles.pChainedPuzzleState;

namespace EOS.Modules.Instances
{
    public sealed class ChainedPuzzleInstanceManager : InstanceManager<ChainedPuzzleInstance>
    {
        public static ChainedPuzzleInstanceManager Current { get; } = new();

        private Dictionary<IntPtr, Action<pCPState, pCPState, bool>> Puzzles_OnStateChange { get; } = new();

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            Puzzles_OnStateChange.Clear();
            base.OnLevelCleanup();
        }

        public override (int dim, int layer, int zone) GetGlobalIndex(ChainedPuzzleInstance instance)
        {
            return instance.m_sourceArea.m_courseNode.m_zone.ToIntTuple();
        }

        public override uint Register((int, int, int) globalZoneIndex, ChainedPuzzleInstance instance)
        {
            uint instanceIndex = base.Register(globalZoneIndex, instance);
            if (instanceIndex != INVALID_INSTANCE_INDEX)
            {
                Puzzles_OnStateChange[instance.Pointer] = null!;
            }

            return instanceIndex;
        }

        public void Add_OnStateChange(ChainedPuzzleInstance instance, Action<pCPState, pCPState, bool> action) => Add_OnStateChange(instance.Pointer, action);

        public void Add_OnStateChange(IntPtr pointer, Action<pCPState, pCPState, bool> action)
        {
            if (Puzzles_OnStateChange.ContainsKey(pointer))
            {
                Puzzles_OnStateChange[pointer] += action;
            }
            else
            {
                EOSLogger.Error($"ChainedPuzzleInstanceManager: passed in pointer is an unregistered ChainedPuzzleInstance, or is not a ChainedPuzzle");
                return;
            }
        }

        public void Remove_OnStateChange(ChainedPuzzleInstance instance, Action<pCPState, pCPState, bool> action) => Remove_OnStateChange(instance.Pointer, action);

        public void Remove_OnStateChange(IntPtr pointer, Action<pCPState, pCPState, bool> action)
        {
            if (Puzzles_OnStateChange.ContainsKey(pointer))
            {
                Puzzles_OnStateChange[pointer] -= action;
            }
            else
            {
                EOSLogger.Error($"ChainedPuzzleInstanceManager: passed in pointer is an unregistered ChainedPuzzleInstance, or is not a ChainedPuzzle");
                return;
            }
        }

        public Action<pCPState, pCPState, bool> Get_OnStateChange(ChainedPuzzleInstance instance) => Get_OnStateChange(instance.Pointer);

        public Action<pCPState, pCPState, bool> Get_OnStateChange(IntPtr pointer) => Puzzles_OnStateChange.TryGetValue(pointer, out var actions) ? actions : null!;
    }
}
