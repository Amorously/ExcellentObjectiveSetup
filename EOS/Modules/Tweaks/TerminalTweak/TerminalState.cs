namespace EOS.Modules.Tweaks.TerminalTweak
{
    public struct TerminalState
    {
        public bool enabled = true;

        public TerminalState() { }

        public TerminalState(bool Enabled) { enabled = Enabled; }

        public TerminalState(TerminalState o) { enabled = o.enabled; }
    }
}
