using AmorLib.Networking.StateReplicators;
using AmorLib.Utils.Extensions;
using EOS.Modules.World.EMP.Handlers;
using UnityEngine;

namespace EOS.Modules.World.EMP
{
    public sealed class PersistentEMP : IEMPSource
    {
        public StateReplicator<PersistentEMPState>? Replicator { get; private set; }
        public ActiveState Status { get; private set; } = ActiveState.DISABLED;
        public Vector3 Position { get; }
        public float Range { get; }
        public float SqrRange { get; }
        public ItemToDisable ItemToDisable { get; }
        public bool IsActive => Status == ActiveState.ENABLED;
        public uint Index { get; }
        public bool InRange(Vector3 point) => point.IsWithinSqrDistance(Position, SqrRange);

        public PersistentEMP(PersistentEMPDefinition def)
        {
            Index = def.pEMPIndex;
            Position = def.Position;
            Range = def.Range;
            SqrRange = Range * Range;
            ItemToDisable = def.ItemToDisable;

            uint allottedID = EOSNetworking.AllotReplicatorID();
            if (allottedID == EOSNetworking.INVALID_ID)
            {
                EOSLogger.Error("pEMP: replicator IDs depleted, cannot setup StateReplicator");
                return;
            }

            Replicator = StateReplicator<PersistentEMPState>.Create(allottedID, new() { status = ActiveState.DISABLED }, LifeTimeType.Session);
            Replicator!.OnStateChanged += OnStateChanged;

        }

        public void Destroy()
        {
            Replicator?.Unload();
            foreach (var handler in EMPLightHandler.AllLights)
            {
                handler.RemoveAffectedBy(this);
            }
        }

        public void ChangeState(ActiveState status)
        {
            EOSLogger.Debug($"ChangeState: pEMP #{Index} changed to state {status}");
            Replicator?.SetState(new() { status = status });
        }

        private void OnStateChanged(PersistentEMPState _, PersistentEMPState state, bool isRecall)
        {
            Status = state.status;
            if (!ItemToDisable.EnvLight) return;

            foreach (var handler in EMPLightHandler.AllLights)
            {
                if (IsActive && InRange(handler.Position)) 
                    handler.AddAffectedBy(this);
                else
                    handler.RemoveAffectedBy(this);
            }            
        }        
    }
}
