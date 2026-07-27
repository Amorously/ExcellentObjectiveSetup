using AmorLib.Utils.JsonElementConverters;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;

namespace EOS.BaseClasses.CustomTerminalDefinition
{
    public class CustomCommand
    {
        public string Command { get; set; } = string.Empty;

        public LocaleText CommandDesc { get; set; } = LocaleText.Empty;

        public List<LocaleTerminalOutput> PostCommandOutputs { get; set; } = new();

        public List<WardenObjectiveEventData> CommandEvents { get; set; } = new();

        public TERM_CommandRule SpecialCommandRule { get; set; } = TERM_CommandRule.Normal;

        public CustomTerminalCommand ToVanillaDataType()
        {
            return new() 
            { 
                Command = Command,
                CommandDesc = new() { UntranslatedText = CommandDesc.ParseTextFragments(), Id = 0u },
                CommandEvents = CommandEvents.ToIl2Cpp(), 
                PostCommandOutputs = PostCommandOutputs.ConvertAll(x => x.ToTerminalOutput()).ToIl2Cpp(),
                SpecialCommandRule = SpecialCommandRule
            };
        }

        public struct LocaleTerminalOutput
        {
            public TerminalLineType LineType { get; set; }
            public LocaleText Output { get; set; }
            public float Time { get; set; }

            public readonly TerminalOutput ToTerminalOutput()
            {
                return new()
                {
                    LineType = LineType,
                    Output = new() { UntranslatedText = Output.ParseTextFragments(), Id = 0u },
                    Time = Time,
                };
            }
        }
    }
}
