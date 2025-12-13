using ChainedPuzzles;
using EOS.BaseClasses;
using GameData;
using LevelGeneration;
using System.Text.Json.Serialization;

namespace EOS.Modules.Objectives.TerminalUplink
{
    public enum UplinkTerminal
    {
        SENDER,
        RECEIVER
    }
    
    public class Terminal // same as BaseDefinition, but redeclare one for readability
    {
        public eDimensionIndex DimensionIndex { get; set; }
        public LG_LayerType Layer { get; set; }
        public LG_LayerType LayerType { get => Layer; set => Layer = value; }
        public eLocalZoneIndex LocalIndex { get; set; }
        public uint InstanceIndex { get; set; } = uint.MaxValue;
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
        public float TimeToStartVerify { set; get; } = 5f;
        public float TimeToCompleteVerify { set; get; } = 6f;
        public float TimeToRestoreFromFail { set; get; } = 6f;
    }

    public class UplinkDefinition : BaseInstanceDefinition
    {
        public bool DisplayUplinkWarning { get; set; } = true;
        public bool SetupAsCorruptedUplink { get; set; } = false;
        public Terminal CorruptedUplinkReceiver { get; set; } = new();
        public bool UseUplinkAddress { get; set; } = true;
        public Terminal UplinkAddressLogPosition { get; set; } = new();
        public uint ChainedPuzzleToStartUplink { set; get; } = 0u;
        public uint NumberOfVerificationRounds { get; set; } = 1u;
        public TimeSettings DefaultTimeSettings { get; set; } = new();
        public List<UplinkRound> RoundOverrides { get; set; } = new() { new() };        
        public List<WardenObjectiveEventData> EventsOnCommence { set; get; } = new(); // same as specifying OnStart event in RoundOverrides with RoundIndex 0
        public List<WardenObjectiveEventData> EventsOnComplete { set; get; } = new(); // same as specifying OnMid event in RoundOverrides with RoundIndex -> last round
    }
}
