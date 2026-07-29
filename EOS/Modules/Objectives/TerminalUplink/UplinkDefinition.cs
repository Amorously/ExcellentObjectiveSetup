using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.BaseClasses.CustomTerminalDefinition;
using EOS.Modules.Tweaks.TerminalTweak;
using GameData;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.TerminalUplink
{
    public enum UplinkTerminal
    {
        SENDER,
        RECEIVER
    }

    public class UplinkDefinition : BaseInstanceDefinition // idk why but the properties are serialized in the wrong order
    {
        [JsonPropertyOrder(0)]
        public int WardenObjectiveIndex { get; set; } = -1;

        [JsonPropertyOrder(1)]
        public bool DisplayUplinkWarning { get; set; } = true;

        [JsonPropertyOrder(2)]
        public bool SetupAsCorruptedUplink { get; set; } = false;

        [JsonPropertyOrder(3)]
        public BaseTerminalDefinition CorruptedUplinkReceiver { get; set; } = new();

        [JsonPropertyOrder(4)]
        public bool UseUplinkAddress { get; set; } = true;

        [JsonPropertyOrder(5)]
        public BaseTerminalDefinition UplinkAddressLogPosition { get; set; } = new();

        [JsonPropertyOrder(6)]
        public bool UseIPv6Addresses { get; set; } = false;

        [JsonPropertyOrder(7)]
        public bool HyphanateCodeWordPrefixes { get; set; } = false;

        [JsonPropertyOrder(8)]
        public bool UseHardCodeWordPrefixes { get; set; } = false;

        [JsonPropertyOrder(9)]
        public SerialGeneratorManager.CodeWordLength CodeWordLength { get; set; } = SerialGeneratorManager.CodeWordLength.Four;

        [JsonPropertyOrder(10)]
        public int CandidateWordsCount { get; set; } = 6;

        [JsonPropertyOrder(11)]
        public uint ChainedPuzzleToStartUplink { get; set; } = 0u;

        [JsonPropertyOrder(12)]
        public uint NumberOfVerificationRounds { get; set; } = 1u;

        [JsonPropertyOrder(13)]
        public TimeSettings DefaultTimeSettings { get; set; } = new();

        [JsonPropertyOrder(14)]
        public List<UplinkRound> RoundOverrides { get; set; } = new() { new() };

        [JsonPropertyOrder(15)]
        public List<WardenObjectiveEventData> EventsOnCommence { get; set; } = new(); // same as specifying OnStart event in RoundOverrides with RoundIndex 0

        [JsonPropertyOrder(16)]
        public List<WardenObjectiveEventData> EventsOnComplete { get; set; } = new(); // same as specifying OnMid event in RoundOverrides with RoundIndex -> last round

        internal void Cleanup()
        {
            ResetFirstRoundOutput();
            RoundOverrides.ForEach(r => r.ChainedPuzzleToEndRoundInstance = null!);
        }

        internal bool FirstRoundOutputted(int roundIndex)
        {
            if (roundIndex == 0 && !_firstRoundOutputted)
                return _firstRoundOutputted = true;
            return false;
        }

        internal void ResetFirstRoundOutput() => _firstRoundOutputted = false;

        private bool _firstRoundOutputted = false;
    }

    public class UplinkRound 
    {
        public int RoundIndex { get; set; } = -1;

        public uint ChainedPuzzleToEndRound { get; set; } = 0u;

        public UplinkTerminal BuildChainedPuzzleOn { get; set; } = UplinkTerminal.SENDER;

        [JsonIgnore]
        public ChainedPuzzleInstance ChainedPuzzleToEndRoundInstance { get; set; } = null!;

        public TimeSettings OverrideTimeSettings { get; set; } = new() 
        {
            TimeToStartVerify = -1f,
            TimeToCompleteVerify = -1f,
            TimeToRestoreFromFail = -1f,
        };

        // trigger is not ignored: 
        // 1 - OnUplinkRound StartWaitingForVerify
        // 2 - OnUplinkVerify Correct, building signature
        public List<WardenObjectiveEventData> EventsOnRound { get; set; } = new();
    }

    public class TimeSettings // using vanilla default value
    {
        public float TimeToStartVerify { get; set; } = 5f;

        public float TimeToCompleteVerify { get; set; } = 6f;

        public float TimeToRestoreFromFail { get; set; } = 6f;
    }
}
