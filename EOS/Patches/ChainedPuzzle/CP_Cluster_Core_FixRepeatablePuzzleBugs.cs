using ChainedPuzzles;
using HarmonyLib;

namespace EOS.Patches.ChainedPuzzle
{
    [HarmonyPatch(typeof(CP_Cluster_Core), nameof(CP_Cluster_Core.OnSyncStateChange))]
    internal static class CP_Cluster_Core_FixRepeatablePuzzleBugs // vanilla bug fix: CP_Cluster_Core.OnPuzzleDone is executed on checkpoint restore
    {
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_CheckEventsOnPuzzleSolved(CP_Cluster_Core __instance, eClusterStatus newStatus, bool isDropinState)
        {
            pClusterState currentState = __instance.m_sync.GetCurrentState();

            if (isDropinState && newStatus == eClusterStatus.Finished) // CP_Cluster_Core checkpoint restore fix
            {
                __instance.m_spline.SetVisible(false);
                for (int k = 0; k < __instance.m_childCores.Length; k++)
                {
                    __instance.m_childCores[k].Deactivate();
                }

                // NOTE: unwanted line in R6 mono
                // HOWEVER I don't know if this would break any shit
                //__instance.OnPuzzleDone?.Invoke(__instance.m_puzzleIndex);
                return false;
            }
            else if (!isDropinState && currentState.status == eClusterStatus.Finished && newStatus == eClusterStatus.SplineReveal) // repeatable command event fix
            {
                __instance.m_spline.Reveal();
                return false;
            }

            return true;
        }
    }
}
