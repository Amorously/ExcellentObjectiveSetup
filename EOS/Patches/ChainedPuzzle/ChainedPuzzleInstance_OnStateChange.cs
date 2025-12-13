using ChainedPuzzles;
using EOS.Modules.Instances;
using HarmonyLib;

namespace EOS.Patches.ChainedPuzzle
{
    [HarmonyPatch(typeof(ChainedPuzzleInstance), nameof(ChainedPuzzleInstance.OnStateChange))]
    internal static class ChainedPuzzleInstance_OnStateChange
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_ChainedPuzzleOnActivationInstance_OnStateChange(ChainedPuzzleInstance __instance, pChainedPuzzleState oldState, pChainedPuzzleState newState, bool isRecall)
        {
            var actions = ChainedPuzzleInstanceManager.Current.Get_OnStateChange(__instance);
            actions?.Invoke(oldState, newState, isRecall);
        }
    }
}
