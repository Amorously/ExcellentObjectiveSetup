using EOS.Modules.Instances;
using HarmonyLib;
using LevelGeneration;
using Player;
using SNetwork;

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
                    EOSLogger.Error($"Inserting something other than PowerCell ({item.PublicName}) into {__instance.m_itemKey}, how?");
                }
            });

            if (PowerGeneratorInstanceManager.Current.IsGCGenerator(__instance)) 
                return;

            PowerGeneratorInstanceManager.Current.Register(__instance);
        }
    }
}
