namespace EOS.Modules.Objectives.TerminalUplink
{
    public enum UplinkStatus
    {
        Unfinished,
        InProgress,
        Finished,
    }

    public struct UplinkState
    {
        public UplinkStatus status { get; set; } = UplinkStatus.Unfinished;
        public int currentRoundIndex { get; set; } = 0;

        public UplinkState() { }    

        public UplinkState(UplinkState o) 
        {
            currentRoundIndex = o.currentRoundIndex;
            status = o.status;
        }
    }
}
