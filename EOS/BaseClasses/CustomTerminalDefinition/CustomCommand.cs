using AmorLib.Utils.JsonElementConverters;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;

namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class CustomCommand
    {
        public string Command { set; get; } = string.Empty;

        public LocaleText CommandDesc { set; get; } = LocaleText.Empty;

        public List<TerminalOutput> PostCommandOutputs { set; get; } = new();

        public List<WardenObjectiveEventData> CommandEvents { set; get; } = new();

        public TERM_CommandRule SpecialCommandRule { set; get; } = TERM_CommandRule.Normal;

        public CustomTerminalCommand ToVanillaDataType()
        {
            return new() 
            { 
                Command = Command,
                CommandDesc = CommandDesc,
                CommandEvents = CommandEvents.ToIl2Cpp(), 
                PostCommandOutputs = PostCommandOutputs.ToIl2Cpp(),
                SpecialCommandRule = SpecialCommandRule
            };
        }
    }
}
