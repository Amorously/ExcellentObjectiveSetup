using GameData;

namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class CustomTerminalZoneSelectionData : GlobalBased
    {
        public eSeedType SeedType { get; set; } = eSeedType.SessionSeed;

        public int TerminalIndex { get; set; } = 0;

        public int StaticSeed { get; set; } = 0;

        public CustomTerminalZoneSelectionData()
        {
            TerminalIndex = Math.Max(0, TerminalIndex);
        }
    }
}
