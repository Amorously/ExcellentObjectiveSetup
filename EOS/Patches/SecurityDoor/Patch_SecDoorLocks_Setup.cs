using EOS.Modules.Tweaks.SecDoorIntText;
using HarmonyLib;
using LevelGeneration;
using Player;

namespace EOS.Patches.SecurityDoor
{
    [HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.Setup), new Type[] { typeof(LG_SecurityDoor) })]
    internal static class Patch_SecDoorLocks_Setup
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_Customize_SecDoor_Interaction_Text(LG_SecurityDoor_Locks __instance)
        {
            if (!SecDoorIntTextOverrideManager.Current.TryGetDefinition(__instance, out var def) || def.GlitchMode == GlitchMode.None)
                return;

            var comp = __instance.gameObject.AddComponent<InteractGlitchComp>();
            comp.Init(def);            

            __instance.m_intCustomMessage.add_OnInteractionSelected((Action<PlayerAgent, bool>)((agent, selected) =>
            {
                if (!agent.IsLocallyOwned) return;
                comp.CanInteract = false;
                comp.enabled = selected;
            }));
            
            __instance.m_intOpenDoor.add_OnInteractionSelected((Action<PlayerAgent, bool>)((agent, selected) =>
            {
                if (!agent.IsLocallyOwned) return;
                comp.CanInteract = true;
                comp.enabled = selected;
            }));
        }
    }
}
