using AmorLib.Networking.StateReplicators;
using LevelGeneration;

namespace EOS.Modules.Tweaks.TerminalTweak
{
    public class TerminalWrapper
    {
        public LG_ComputerTerminal Terminal { get; private set; } = null!;
        public StateReplicator<TerminalState>? Replicator { get; private set; }

        public TerminalWrapper(LG_ComputerTerminal term, uint replicatorID)
        {
            if (term == null || replicatorID == EOSNetworking.INVALID_ID) 
                return;

            Terminal = term;
            Replicator = StateReplicator<TerminalState>.Create(replicatorID, new() { enabled = true }, LifeTimeType.Session);
            Replicator!.OnStateChanged += OnStateChanged;
        }

        public void ChangeState(bool enabled)
        {
            Replicator?.SetState(new() { enabled = enabled });
        }

        private void OnStateChanged(TerminalState oldState, TerminalState state, bool isRecall)
        {
            if (oldState.enabled == state.enabled) return;
            bool active = state.enabled;

            Terminal.OnProximityExit();
            var interact = Terminal.GetComponentInChildren<Interact_ComputerTerminal>(true);
            if (interact != null)
            {
                interact.enabled = active;
                interact.SetActive(active);
            }

            Terminal.m_interfaceScreen.SetActive(active);
            Terminal.m_loginScreen.SetActive(active);

            if (Terminal.m_text != null)
                Terminal.m_text.enabled = active;

            if (!active)
            {
                var interactionSource = Terminal.m_localInteractionSource;
                if (interactionSource?.FPItemHolder?.InTerminalTrigger == true)
                {
                    Terminal.ExitFPSView();
                }
            }
        }
    }
}
