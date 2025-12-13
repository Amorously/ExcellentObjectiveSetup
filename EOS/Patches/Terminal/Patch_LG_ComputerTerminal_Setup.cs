using AmorLib.Utils;
using EOS.Modules.Instances;
using EOS.Modules.Tweaks.TerminalPosition;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace EOS.Patches.Terminal
{
    [HarmonyPatch(typeof(LG_ComputerTerminal), nameof(LG_ComputerTerminal.Setup))]
    internal static class Patch_LG_ComputerTerminal_Setup
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_LG_ComputerTerminal_Setup(LG_ComputerTerminal __instance)
        {
            uint instanceIndex = TerminalInstanceManager.Current.Register(__instance);

            if (__instance.ConnectedReactor != null) return; // disallow changing position of reactor terminal

            var globalIndex = TerminalInstanceManager.Current.GetGlobalIndex(__instance);
            var posOverride = TerminalPositionOverrideManager.Current.GetDefinition(globalIndex, instanceIndex);
            if (posOverride == null) return; // modify terminal position

            Vector3 position = posOverride.Position.ToVector3();
            Quaternion rotation = posOverride.Rotation.ToQuaternion();
            if (position == Vector3.zero) return;           
            
            __instance.transform.position = position;
            __instance.transform.rotation = rotation;
            EOSLogger.Debug($"TerminalPositionOverride: {posOverride}");            
            
            var newNode = CourseNodeUtil.GetCourseNode(position, Dimension.GetDimensionFromPos(position).DimensionIndex);
            if (__instance.SpawnNode != newNode)
                return; // instantiate new prefab and update node
        }
    }
}
