using AmorLib.Utils.JsonElementConverters;
using EOS.BaseClasses;
using GameData;
using LevelGeneration;
using Localization;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.Reactor
{
    public enum EOSReactorVerificationType
    {
        NORMAL,
        BY_SPECIAL_COMMAND,
        BY_WARDEN_EVENT
    }

    public class ReactorStartupOverride : BaseReactorDefinition
    {
        public bool StartupOnDrop { get; set; } = false;

        [JsonIgnore]
        public WardenObjectiveDataBlock ObjectiveDB { get; set; } = null!;

        public List<WaveOverride> Overrides { set; get; } = new() { new() };
    }

    public class WaveOverride
    {
        public int WaveIndex { get; set; } = -1;

        public EOSReactorVerificationType VerificationType { get; set; } = EOSReactorVerificationType.NORMAL;

        public bool HideVerificationTimer { get; set; } = false;

        public bool ChangeVerifyZone { get; set; } = false;

        public BaseInstanceDefinition VerifyZone { get; set; } = new();

        public bool UseCustomVerifyText { get; set; } = false;

        public LocaleText VerifySequenceText { get; set; } = LocaleText.Empty;

        [JsonIgnore]
        public LG_ComputerTerminal VerifyTerminal { get; set; } = null!;
    }
}
