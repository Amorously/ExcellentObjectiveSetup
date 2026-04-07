namespace EOS.Modules.World.EMP
{
    public struct PersistentEMPState
    {
        public ActiveState status { get; set; } = ActiveState.DISABLED;

        public PersistentEMPState(PersistentEMPState p) { status = p.status; }

        public PersistentEMPState(ActiveState status) { this.status = status; }
    }
}
