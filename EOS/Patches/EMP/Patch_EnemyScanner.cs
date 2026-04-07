using AK;
using EOS.Modules.World.EMP.Handlers;
using Gear;
using HarmonyLib;
using UnityEngine;

namespace EOS.Patches.EMP
{
    [HarmonyPatch(typeof(EnemyScanner))]
    internal static class Patch_EnemyScanner
    {
        [HarmonyPatch(nameof(EnemyScanner.UpdateDetectedEnemies))]
        [HarmonyPrefix]
        private static bool Pre_UpdateDetectedEnemies()
        {
            return EMPBioTrackerHandler.Instance?.IsEMPed() != true;
        }
        
        [HarmonyPatch(nameof(EnemyScanner.UpdateTagProgress))]
        [HarmonyPrefix]
        private static bool Pre_UpdateTagProgress(EnemyScanner __instance)
        {
            if (EMPBioTrackerHandler.Instance?.IsEMPed() != true)
            {
                __instance.m_screen.SetStatusText("Ready to tag");
                __instance.m_screen.SetGuixColor(Color.red);
                return true;
            }

            __instance.Sound.Post(EVENTS.BIOTRACKER_TOOL_LOOP_STOP);
            __instance.m_screen.SetStatusText("ERROR");
            __instance.m_progressBar.SetProgress(1f);
            __instance.m_screen.SetGuixColor(Color.yellow);
            return false;
        }
    }
}
