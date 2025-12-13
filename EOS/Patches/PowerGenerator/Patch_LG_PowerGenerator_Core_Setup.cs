using AmorLib.Utils;
using EOS.Modules.Instances;
using EOS.Modules.Objectives.IndividualGenerator;
using HarmonyLib;
using LevelGeneration;
using Player;
using SNetwork;
using UnityEngine;

namespace EOS.Patches.PowerGenerator
{
    [HarmonyPatch]
    internal static class Patch_LG_PowerGenerator_Core_Setup
    {
        [HarmonyPatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.Setup))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_PowerGenerator_Setup(LG_PowerGenerator_Core __instance)
        {
            // do some minor vanilla bug fix
            __instance.m_powerCellInteraction.AttemptCarryItemInsert += new Action<SNet_Player, Item>((p, item) =>
            {
                if (PlayerBackpackManager.TryGetItemInLevelFromItemData(item.Get_pItemData(), out var itemInLevel))
                {
                    var cell = itemInLevel.Cast<ItemInLevel>();
                    cell.CanWarp = false;
                }
                else
                {
                    EOSLogger.Error($"Inserting sth other than PowerCell ({item.PublicName}) into {__instance.m_itemKey}, how?");
                }
            });

            if (PowerGeneratorInstanceManager.Current.IsGCGenerator(__instance)) 
                return;

            PowerGeneratorInstanceManager.Current.Register(__instance);

            if (!IndividualGeneratorObjectiveManager.Current.TryGetDefinition(__instance, out var def))
                return;

            Vector3 position = def.Position.ToVector3();
            Quaternion rotation = def.Rotation.ToQuaternion();
            if (position != Vector3.zero)
            {
                __instance.transform.position = position;
                __instance.transform.rotation = rotation;
                __instance.m_sound.UpdatePosition(position);
                EOSLogger.Debug($"LG_PowerGenerator_Core: modified position / rotation");
            }

            var newNode = CourseNodeUtil.GetCourseNode(position, Dimension.GetDimensionFromPos(position).DimensionIndex);
            //if (__instance.SpawnNode != newNode)
            //    return; // instantiate new prefab and update node

            __instance.SetCanTakePowerCell(def.ForceAllowPowerCellInsertion);
            EOSLogger.Debug($"LG_PowerGenerator_Core: overriden, instance {def}");
        }
        
        [HarmonyPatch(typeof(LG_PowerGenerator_Core), nameof(LG_PowerGenerator_Core.SyncStatusChanged))]
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_SyncStatusChanged(LG_PowerGenerator_Core __instance, pPowerGeneratorState state, bool isDropinState)
        {
            if (!IndividualGeneratorObjectiveManager.Current.TryGetDefinition(__instance, out var def))
                return;
            if (def.EventsOnInsertCell == null || state.status != ePowerGeneratorStatus.Powered || isDropinState) 
                return;

            EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnInsertCell);
        }
    }
}
