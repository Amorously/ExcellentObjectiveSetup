using GameData;

namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class CustomTerminalZoneSelectionData : GlobalBased
    {
        public eSeedType SeedType { set; get; } = eSeedType.SessionSeed;

        public int TerminalIndex { set; get; } = 0;

        public int StaticSeed { set; get; } = 0;

        public CustomTerminalZoneSelectionData()
        {
            TerminalIndex = Math.Max(0, TerminalIndex);
        }
    }
}
