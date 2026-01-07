using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using EOS.BaseClasses;
using GameData;
using LevelGeneration;
using Player;
using SNetwork;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EOS.Modules.Tweaks.DimensionWarp
{
    public sealed class DimensionWarpManager : GenericExpeditionDefinitionManager<DimensionWarpDefinition, DimensionWarpManager>
    {
        public enum WarpEventType
        {
            WarpTeam = 160,
            WarpRange = 161,
            WarpItemsInZone = 162,
        }

        private readonly ImmutableList<Vector3> _lookDirs = ImmutableList.Create(new[]
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right 
        });

        protected override string DEFINITION_NAME => "DimensionWarp";

        static DimensionWarpManager()
        {
            EOSWardenEventManager.AddEventDefinition(WarpEventType.WarpTeam.ToString(), (uint)WarpEventType.WarpTeam, WarpTeam);
            EOSWardenEventManager.AddEventDefinition(WarpEventType.WarpRange.ToString(), (uint)WarpEventType.WarpRange, WarpRange);
            EOSWardenEventManager.AddEventDefinition(WarpEventType.WarpItemsInZone.ToString(), (uint)WarpEventType.WarpItemsInZone, WarpItemsInZone);
        }

        public bool TryGetWarpDefinition(string worldEventObjectFilter, [MaybeNullWhen(false)] out DimensionWarpDefinition warpDef)
        {
            warpDef = null;
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel || !GenericExpDefinitions.TryGetValue(CurrentMainLevelLayout, out var def))
                return false;

            warpDef = def.Definitions.Find(w => w.WorldEventObjectFilter == worldEventObjectFilter);
            return warpDef != null;
        }

        private static void WarpTeam(WardenObjectiveEventData e)
        {
            if (!Current.TryGetWarpDefinition(e.WorldEventObjectFilter, out var def))
            {
                EOSLogger.Error($"WarpTeam: def WorldEventObjectFilter '{e.WorldEventObjectFilter}' is not defined");
                return;
            }
                
            Current.WarpTeam(def);
        }

        public void WarpTeam(DimensionWarpDefinition def)
        {
            var warpLocations = def.Locations;
            if (warpLocations.Count < 1)
            {
                EOSLogger.Error("WarpTeam: no warp locations found");
                return;
            }

            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            int positionIndex = localPlayer.PlayerSlotIndex % warpLocations.Count;
            Vector3 warpPosition = warpLocations[positionIndex].Position;
            int lookDirIndex = warpLocations[positionIndex].LookDir % _lookDirs.Count;
            Vector3 lookDir = _lookDirs[lookDirIndex];

            int itemPositionIdx = 0;
            List<SentryGunInstance> sentryGunToWarp = new();
            foreach (var warpable in Dimension.WarpableObjects)
            {
                var sentryGun = warpable.TryCast<SentryGunInstance>();
                if (sentryGun?.LocallyPlaced == true)
                {
                    sentryGunToWarp.Add(sentryGun);        
                    continue;
                }

                if (SNet.IsMaster && def.OnWarp.WarpTeam_WarpAllWarpableBigPickupItems)
                {
                    var itemInLevel = warpable.TryCast<ItemInLevel>();
                    if (itemInLevel?.CanWarp == true && itemInLevel.internalSync.GetCurrentState().placement.droppedOnFloor)
                    {
                        var itemPosition = warpLocations[itemPositionIdx].Position;
                        WarpItem(itemInLevel, def.DimensionIndex, itemPosition);
                        itemPositionIdx = (itemPositionIdx + 1) % warpLocations.Count;
                    }
                }
            }

            sentryGunToWarp.ForEach(sentryGun => sentryGun.m_sync.WantItemAction(sentryGun.Owner, SyncedItemAction_New.PickUp));

            if (!localPlayer.TryWarpTo(def.DimensionIndex, warpPosition, lookDir, true))
            {
                EOSLogger.Error($"WarpTeam: TryWarpTo failed. Position: {warpPosition}, playerSlotIndex: {localPlayer.PlayerSlotIndex}, warpLocationIndex: {positionIndex}");
            }
        }

        private static void WarpRange(WardenObjectiveEventData e)
        {
            if (!Current.TryGetWarpDefinition(e.WorldEventObjectFilter, out var def))
            {
                EOSLogger.Error($"WarpTeam: def WorldEventObjectFilter '{e.WorldEventObjectFilter}' is not defined");
                return;
            }

            Current.WarpRange(def, e.Position, e.FogTransitionDuration);
        }

        public void WarpRange(DimensionWarpDefinition def, Vector3 rangeOrigin, float range)
        {            
            var warpLocations = def.Locations;
            if (warpLocations.Count < 1)
            {
                EOSLogger.Error("WarpAlivePlayersInRange: no warp locations found");
                return;
            }
            
            var localPlayer = PlayerManager.GetLocalPlayerAgent();
            int positionIndex = localPlayer.PlayerSlotIndex % warpLocations.Count;
            Vector3 warpPosition = warpLocations[positionIndex].Position;
            int lookDirIndex = warpLocations[positionIndex].LookDir % _lookDirs.Count;
            Vector3 lookDir = _lookDirs[lookDirIndex];
            float sqrRange = range * range;

            int itemPositionIdx = 0;
            List<SentryGunInstance> sentryGunToWarp = new();
            foreach (var warpable in Dimension.WarpableObjects)
            {
                var sentryGun = warpable.TryCast<SentryGunInstance>();
                if (sentryGun != null)
                {
                    bool sentryOwnerCanWarp = sentryGun.LocallyPlaced && sentryGun.Owner.Alive && rangeOrigin.IsWithinSqrDistance(sentryGun.Owner.Position, sqrRange);
                    bool sentryCanWarp = def.OnWarp.WarpRange_WarpDeployedSentryOutsideRange || rangeOrigin.IsWithinSqrDistance(sentryGun.transform.position, sqrRange);
                    if (sentryOwnerCanWarp && sentryCanWarp)
                    {
                        sentryGunToWarp.Add(sentryGun);
                        continue;
                    }
                }

                if (SNet.IsMaster)
                {
                    var itemInLevel = warpable.TryCast<ItemInLevel>();
                    if (itemInLevel != null && itemInLevel.transform.position.IsWithinSqrDistance(rangeOrigin, sqrRange))
                    {
                        var itemPosition = warpLocations[itemPositionIdx].Position;
                        WarpItem(itemInLevel, def.DimensionIndex, itemPosition);
                        itemPositionIdx = (itemPositionIdx + 1) % warpLocations.Count;
                    }
                }
            }

            sentryGunToWarp.ForEach(sentryGun => sentryGun.m_sync.WantItemAction(sentryGun.Owner, SyncedItemAction_New.PickUp));

            bool playerCanWarp = localPlayer.Alive && rangeOrigin.IsWithinSqrDistance(localPlayer.Position, sqrRange);
            if (!localPlayer.TryWarpTo(def.DimensionIndex, warpPosition, lookDir, true))
            {
                EOSLogger.Error($"WarpAlivePlayersInRange: TryWarpTo failed, Position: {warpPosition}");
            }
        }

        private static void WarpItemsInZone(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster)
                return;

            if (!Current.TryGetWarpDefinition(e.WorldEventObjectFilter, out var def))
            {
                EOSLogger.Error($"WarpTeam: def WorldEventObjectFilter '{e.WorldEventObjectFilter}' is not defined");
                return;
            }

            WarpItemsInZone(def, e.DimensionIndex, e.Layer, e.LocalIndex);
        }

        public static void WarpItemsInZone(DimensionWarpDefinition def, eDimensionIndex dimensionIndex, LG_LayerType layer, eLocalZoneIndex localIndex)
        {
            var warpLocations = def.Locations;
            if (warpLocations.Count < 1)
            {
                EOSLogger.Error("WarpItemsInZone: no warp locations found");
                return;
            }

            int itemPositionIdx = 0;
            foreach (var warpable in Dimension.WarpableObjects)
            {
                var itemInLevel = warpable.TryCast<ItemInLevel>();
                if (itemInLevel == null)
                    continue;

                bool droppedOnFloor = itemInLevel.internalSync.GetCurrentState().placement.droppedOnFloor;
                var globalIndex = itemInLevel.CourseNode.m_zone.ToIntTuple();
                var globalIndex2 = GlobalIndexUtil.ToIntTuple(dimensionIndex, layer, localIndex);
                bool itemCanWarp = !def.OnWarp.WarpItemsInZone_OnlyWarpWarpable || itemInLevel.CanWarp;

                if (droppedOnFloor && globalIndex == globalIndex2 && itemCanWarp)
                {
                    var itemPosition = warpLocations[itemPositionIdx].Position;
                    WarpItem(itemInLevel, dimensionIndex, itemPosition);
                }
                itemPositionIdx = (itemPositionIdx + 1) % warpLocations.Count;                
            }
        }

        public static void WarpItem(ItemInLevel item, eDimensionIndex warpToDim, Vector3 warpToPosition)
        {
            var courseNode = CourseNodeUtil.GetCourseNode(warpToPosition, warpToDim);
            if (courseNode == null)
            {
                EOSLogger.Error("WarpItem: cannot find course node for item to warp");
                return;
            }
            
            item?.GetSyncComponent()?.AttemptPickupInteraction
            (
                ePickupItemInteractionType.Place,
                null,
                item.pItemData.custom,
                warpToPosition,
                Quaternion.identity,
                courseNode,
                true,
                true
            );
        }
    }
}
