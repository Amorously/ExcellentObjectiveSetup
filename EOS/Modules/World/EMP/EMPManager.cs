using AmorLib.API;
using AmorLib.Utils.Extensions;
using EOS.BaseClasses;
using EOS.Modules.World.EMP.Handlers;
using GameData;
using Gear;
using GTFO.API.Utilities;
using Player;
using SNetwork;
using UnityEngine;

namespace EOS.Modules.World.EMP
{
    public sealed class EMPManager : GenericExpeditionDefinitionManager<PersistentEMPDefinition, EMPManager>
    {
        public enum EMPEventType
        {
            Instant_Shock = 300,
            Toggle_PEMP_State = 301
        }

        protected override string DEFINITION_NAME => "PersistentEMP";        

        internal readonly List<EMPShock> ActiveShocks = new();
        internal readonly Dictionary<uint, PersistentEMP> PersistentEMPs = new();
        internal static System.Random Rand = new();
        internal static Action<GearPartFlashlight>? FlashlightWielded;
        internal static Action<InventorySlot>? InventoryWielded;

        static EMPManager()
        {
            EOSWardenEventManager.AddEventDefinition(EMPEventType.Instant_Shock.ToString(), (uint)EMPEventType.Instant_Shock, InstantShock);
            EOSWardenEventManager.AddEventDefinition(EMPEventType.Toggle_PEMP_State.ToString(), (uint)EMPEventType.Toggle_PEMP_State, TogglePersistentEMPState);
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            base.FileChanged(e);
            OnBuildStart();
        }

        protected override void OnBuildStart()
        {
            OnLevelCleanup();
            if (!GenericExpDefinitions.TryGetValue(CurrentMainLevelLayout, out var def)) return;
            def.Definitions.ForEach(InitPersistentEMP);
        }

        protected override void OnBuildDone()
        {
            foreach (var worker in LightAPI.GetLightWorkersInDimension(Enum.GetValues<eDimensionIndex>()))
            {
                worker.Light.gameObject.AddComponent<EMPController>().AssignHandler(new EMPLightHandler(worker));
            }
        }

        protected override void OnLevelCleanup()
        {
            ActiveShocks.Clear();
            PersistentEMPs.ForEachValue(pEMP => pEMP.Destroy());
            PersistentEMPs.Clear();
        }

        private void InitPersistentEMP(PersistentEMPDefinition def)
        {
            PersistentEMPs[def.pEMPIndex] = new PersistentEMP(def);
            EOSLogger.Debug($"EMP: PersistentEMP #{def.pEMPIndex} initialized");
        }

        internal void RemoveInactiveShocks()
        {
            ActiveShocks.RemoveAll(s => !s.IsActive);
        }

        public bool IsEMPOnPlayerMap()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) 
                return false;

            var player = PlayerManager.GetLocalPlayerAgent();
            if (player == null) return false;
            Vector3 pos = player.Position;

            foreach (var shock in ActiveShocks)
            {
                if (shock.IsActive && shock.InRange(pos))
                    return true;                
            }

            foreach (var pEMP in PersistentEMPs.Values)
            {
                if (pEMP.IsActive && pEMP.ItemToDisable.Map && pEMP.InRange(pos)) 
                    return true;                 
            }

            return false;
        }

        private static void InstantShock(WardenObjectiveEventData e)
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) 
                return;            

            var shock = new EMPShock(e.Position, e.FogTransitionDuration, Clock.Time + e.Duration);
            foreach (var handler in EMPHandler.All)
            {
                if (shock.InRange(handler.Position))
                {
                    handler.AddAffectedBy(shock);
                }
            }

            Current.ActiveShocks.Add(shock);
        }

        private static void TogglePersistentEMPState(WardenObjectiveEventData e)
        {
            uint index = (uint)e.Count;

            if (!Current.PersistentEMPs.TryGetValue(index, out var pEMP))
            {
                EOSLogger.Error($"TogglepEMPState: no pEMP with index #{index} is defined for this level!");
                return;
            }

            if (SNet.IsMaster)
                pEMP.ChangeState(e.Enabled ? ActiveState.ENABLED : ActiveState.DISABLED);
        }

        internal static float RandRange(float min, float max) => min + Rand.NextSingle() * (max - min);

        internal static bool RandCoin(int oneInX = 2) => Rand.Next(0, oneInX) == 0;
    }
}
