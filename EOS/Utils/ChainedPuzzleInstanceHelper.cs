using ChainedPuzzles;
using EOS.Modules.Instances;
using SNetwork;
using pCPState = ChainedPuzzles.pChainedPuzzleState;

namespace EOS.Utils
{
    public static class ChainedPuzzleInstanceManagerHelper
    {
        public static void Add_OnStateChange(this ChainedPuzzleInstance instance, Action<pCPState, pCPState, bool> action) => ChainedPuzzleInstanceManager.Current.Add_OnStateChange(instance, action);

        public static void Remove_OnStateChange(this ChainedPuzzleInstance instance, Action<pCPState, pCPState, bool> action) => ChainedPuzzleInstanceManager.Current.Remove_OnStateChange(instance, action);

        public static void ResetProgress(this ChainedPuzzleInstance chainedPuzzle)
        {
            if (chainedPuzzle.Data.DisableSurvivalWaveOnComplete)
            {
                chainedPuzzle.m_sound = new(chainedPuzzle.m_parent.position);
            }

            foreach (var IChildCore in chainedPuzzle.m_chainedPuzzleCores)
            {
                ResetChild(IChildCore);
            }

            if (SNet.IsMaster)
            {
                var oldState = chainedPuzzle.m_stateReplicator.State;
                var newState = new pChainedPuzzleState()
                {
                    status = eChainedPuzzleStatus.Disabled,
                    currentSurvivalWave_EventID = oldState.currentSurvivalWave_EventID,
                    isSolved = false,
                    isActive = false,
                };
                chainedPuzzle.m_stateReplicator.InteractWithState(newState, new() { type = eChainedPuzzleInteraction.Deactivate });
            }

            static void ResetChild(iChainedPuzzleCore iCore)
            {
                var bioCore = iCore.TryCast<CP_Bioscan_Core>();
                if (bioCore != null)
                {
                    bioCore.m_spline.SetVisible(false);
                    var scanner = bioCore.PlayerScanner.Cast<CP_PlayerScanner>();
                    scanner.ResetScanProgression(0.0f);
                    bioCore.Deactivate();
                    return;
                }
                
                var clusterCore = iCore.TryCast<CP_Cluster_Core>();
                if (clusterCore == null)
                {
                    EOSLogger.Error($"ResetChild: found iChainedPuzzleCore that is neither CP_Bioscan_Core nor CP_Cluster_Core...");
                    return;
                }

                clusterCore.m_spline.SetVisible(false);
                foreach (var child in clusterCore.m_childCores)
                {
                    ResetChild(child);
                }
                clusterCore.Deactivate();
            }
        }
    }
}
