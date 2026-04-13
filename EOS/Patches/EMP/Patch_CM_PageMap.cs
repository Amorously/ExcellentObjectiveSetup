using CellMenu;
using EOS.Modules.World.EMP;
using HarmonyLib;

namespace EOS.Patches.EMP
{
    [HarmonyPatch]
    internal static class Patch_CM_PageMap
    {        
        [HarmonyPatch(typeof(CM_PageMap), nameof(CM_PageMap.UpdatePlayerData))]    
        [HarmonyPostfix]
        [HarmonyAfter("dev.aurirex.gtfo.dimensionmaps")]
        [HarmonyWrapSafe]
        private static void Post_UpdatePlayerData()
        {
            var map = MainMenuGuiLayer.Current.PageMap;
            if (map == null || RundownManager.ActiveExpedition == null || GameStateManager.CurrentStateName != eGameStateName.InLevel) 
                return;

            bool isEMP = EMPManager.Current.IsEMPOnPlayerMap();
            map.SetMapVisualsIsActive(!isEMP);
            map.SetMapDisconnetedTextIsActive(isEMP);
        }
    }
}
