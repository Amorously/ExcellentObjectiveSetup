using GameData;
using HarmonyLib;
using LevelGeneration;
using Localization;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch]
    internal static class Reactor_Update
    {
        private static bool _checked = false;
        private static TextDataBlock shutdownVerification_GUIText = null!;

        [HarmonyPatch(typeof(LG_WardenObjective_Reactor), nameof(LG_WardenObjective_Reactor.Update))]
        [HarmonyPrefix]
        private static bool Pre_LG_WardenObjective_Reactor_Update(LG_WardenObjective_Reactor __instance)
        {
            // overwrite Update for eReactorStatus.Shutdown_waitForVerify
            if (__instance.m_isWardenObjective || __instance.m_currentState.status != eReactorStatus.Shutdown_waitForVerify) 
                return true;

            if (!_checked)
            {
                shutdownVerification_GUIText = GameDataBlockBase<TextDataBlock>.GetBlock("InGame.ExtraObjectiveSetup_ReactorShutdown.SecurityVerificationRequired");
                _checked = true;
            }

            string displayText;
            if (__instance.m_currentWaveData.HasVerificationTerminal)
            {
                displayText = string.Format(shutdownVerification_GUIText != null ? Text.Get(shutdownVerification_GUIText.persistentID) : "SECURITY VERIFICATION REQUIRED. USE COMMAND <color=orange>REACTOR_VERIFY</color> AND FIND CODE ON <color=orange>{0}</color>.", __instance.m_currentWaveData.VerificationTerminalSerial);
            }
            else
            {
                displayText = string.Format(Text.Get(1107U), "<color=orange>" + __instance.CurrentStateOverrideCode + "</color>");
            }

            __instance.SetGUIMessage(true, displayText, ePUIMessageStyle.Warning, false);
            return false;
        }
    }
}
