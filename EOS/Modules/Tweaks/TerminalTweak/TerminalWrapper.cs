using AmorLib.Networking.StateReplicators;
using LevelGeneration;
using Player;
using SNetwork;

namespace EOS.Modules.Tweaks.TerminalTweak
{
    public class TerminalWrapper
    {
        public LG_ComputerTerminal Terminal { get; private set; } = null!;

        public StateReplicator<TerminalState> StateReplicator { get; private set; } = null!;

        public static TerminalWrapper Instantiate(LG_ComputerTerminal term, uint replicatorID)
        {
            if (term == null || replicatorID == EOSNetworking.INVALID_ID) return null!;

            var wrapper = new TerminalWrapper
            {
                Terminal = term,
                StateReplicator = StateReplicator<TerminalState>.Create(replicatorID, new() { enabled = true }, LifeTimeType.Session)!
            };
            wrapper.StateReplicator.OnStateChanged += wrapper.OnStateChanged;

            return wrapper;
        }

        public void ChangeState(bool enabled)
        {
            ChangeStateUnsynced(enabled);
            if (SNet.IsMaster)
            {
                StateReplicator.SetState(new() { enabled = enabled });
            }
        }

        private void ChangeStateUnsynced(bool enabled)
        {
            //EOSLogger.Debug($"{lgTerminal.ItemKey} state, enabled: {enabled}");

            Terminal.OnProximityExit();
            Interact_ComputerTerminal interact = Terminal.GetComponentInChildren<Interact_ComputerTerminal>(true);
            bool active = enabled;

            if (interact != null)
            {
                interact.enabled = active;
                interact.SetActive(active);
            }

            Terminal.m_interfaceScreen.SetActive(active);
            Terminal.m_loginScreen.SetActive(active);

            if (Terminal.m_text != null)
            {
                Terminal.m_text.enabled = active;
            }

            if (!active)
            {
                PlayerAgent interactionSource = Terminal.m_localInteractionSource;
                if (interactionSource != null && interactionSource.FPItemHolder.InTerminalTrigger)
                {
                    Terminal.ExitFPSView();
                }
            }

        }

        private void OnStateChanged(TerminalState oldState, TerminalState newState, bool isRecall)
        {
            if (!isRecall) return;

            ChangeStateUnsynced(newState.enabled);
        }
    }
}
