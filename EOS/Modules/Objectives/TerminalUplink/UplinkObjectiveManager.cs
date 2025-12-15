using AmorLib.Networking.StateReplicators;
using AmorLib.Utils;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;
using Localization;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Objectives.TerminalUplink
{
    internal sealed class UplinkObjectiveManager: InstanceDefinitionManager<UplinkDefinition>
    {
        protected override string DEFINITION_NAME { get; } = "TerminalUplink";
        
        public static UplinkObjectiveManager Current { get; private set; } = new();

        private TextDataBlock UplinkAddrLogContentBlock = null!;
        private readonly Dictionary<IntPtr, StateReplicator<UplinkState>> _stateReplicators = new();
        private readonly List<UplinkRound> _builtRoundPuzzles = new();

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnBuildDone()
        {
            if (!Definitions.ContainsKey(CurrentMainLevelLayout)) return;
            UplinkAddrLogContentBlock ??= GameDataBlockBase<TextDataBlock>.GetBlock("InGame.UplinkTerminal.UplinkAddrLog");
            Definitions[CurrentMainLevelLayout].Definitions.ForEach(Build);
        }

        protected override void OnLevelCleanup()
        {
            _builtRoundPuzzles.ForEach(r => { r.ChainedPuzzleToEndRoundInstance = null!; });
            _builtRoundPuzzles.Clear();
            _stateReplicators.Clear();
        }

        protected override void AddDefinitions(InstanceDefinitionsForLevel<UplinkDefinition> definitions)
        {
            // because we have chained puzzles, sorting is necessary to preserve chained puzzle instance order.
            Sort(definitions);
            definitions.Definitions.ForEach(u => u.RoundOverrides.Sort((r1, r2) => r1.RoundIndex.CompareTo(r2.RoundIndex)));
            base.AddDefinitions(definitions);
        }

        public bool TryGetDefinition(LG_ComputerTerminal term, [MaybeNullWhen(false)] out UplinkDefinition definition)
        {
            var (globalIndex, instanceIndex) = TerminalInstanceManager.Current.GetGlobalInstance(term);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }

        private void Build(UplinkDefinition def)
        {
            if(!TerminalInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var uplinkTerminal)) 
                return;

            if (uplinkTerminal.m_isWardenObjective && uplinkTerminal.UplinkPuzzle != null)
            {
                EOSLogger.Error($"BuildUplink: terminal uplink already built (by vanilla or custom build), aborting!");
                return;
            }

            if (def.SetupAsCorruptedUplink)
            {
                if (!TerminalInstanceManager.Current.TryGetInstance(def.CorruptedUplinkReceiver, out var receiver))
                {
                    EOSLogger.Error("BuildUplink: SetupAsCorruptedUplink specified but didn't find the receiver terminal!");
                    return;
                }

                if (receiver.Pointer == uplinkTerminal.Pointer)
                {
                    EOSLogger.Error("BuildUplink: Don't specify uplink sender and receiver on the same terminal");
                    return;
                }

                uplinkTerminal.CorruptedUplinkReceiver = receiver;
                receiver.CorruptedUplinkReceiver = uplinkTerminal; // need to set on both side
            }

            uplinkTerminal.UplinkPuzzle = new TerminalUplinkPuzzle();
            SetupUplinkPuzzle(uplinkTerminal.UplinkPuzzle, uplinkTerminal, def);
            uplinkTerminal.UplinkPuzzle.OnPuzzleSolved += new Action(() =>
            {
                EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnComplete);
            });

            uplinkTerminal.m_command.AddCommand
            (
                uplinkTerminal.CorruptedUplinkReceiver == null ? TERM_Command.TerminalUplinkConnect : TERM_Command.TerminalCorruptedUplinkConnect, 
                def.UseUplinkAddress ? "UPLINK_CONNECT" : "UPLINK_ESTABLISH", new LocalizedText() 
                {
                    UntranslatedText = Text.Get(3914968919),
                    Id = 3914968919
                }
            );

            uplinkTerminal.m_command.AddCommand(TERM_Command.TerminalUplinkVerify, "UPLINK_VERIFY", new LocalizedText()
            {
                UntranslatedText = Text.Get(1728022075),
                Id = 1728022075
            });

            if (def.UseUplinkAddress)
            {
                EOSLogger.Debug($"BuildUplinkOverride: UseUplinkAddress");
                if (!TerminalInstanceManager.Current.TryGetInstance(def.UplinkAddressLogPosition, out var addressLogTerminal))
                {
                    EOSLogger.Error($"BuildUplinkOverride: didn't find the terminal to put the uplink address log, will put on uplink terminal");
                    addressLogTerminal = uplinkTerminal;
                }

                addressLogTerminal.AddLocalLog(new TerminalLogFileData()
                {
                    FileName = $"UPLINK_ADDR_{uplinkTerminal.m_serialNumber}.LOG",
                    FileContent = new LocalizedText() 
                    { 
                        UntranslatedText = string.Format(UplinkAddrLogContentBlock != null ? Text.Get(UplinkAddrLogContentBlock.persistentID) : "Available uplink address for TERMINAL_{0}: {1}", uplinkTerminal.m_serialNumber, uplinkTerminal.UplinkPuzzle.TerminalUplinkIP),
                        Id = 0
                    }
                }, true);

                addressLogTerminal.m_command.ClearOutputQueueAndScreenBuffer();
                addressLogTerminal.m_command.AddInitialTerminalOutput();
            }

            if (def.ChainedPuzzleToStartUplink != 0)
            {                
                if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(def.ChainedPuzzleToStartUplink, out var block))
                {
                    EOSLogger.Error($"BuildTerminalUplink: ChainedPuzzleToActive with id {def.ChainedPuzzleToStartUplink} is specified but no ChainedPuzzleDataBlock definition is found... Won't build");
                    uplinkTerminal.m_chainPuzzleForWardenObjective = null;
                }
                else
                {
                    uplinkTerminal.m_chainPuzzleForWardenObjective = ChainedPuzzleManager.CreatePuzzleInstance
                    (
                        block,
                        uplinkTerminal.SpawnNode.m_area,
                        uplinkTerminal.m_wardenObjectiveSecurityScanAlign.position,
                        uplinkTerminal.m_wardenObjectiveSecurityScanAlign
                    );

                    if (def.SetupAsCorruptedUplink)
                    {
                        uplinkTerminal.m_chainPuzzleForWardenObjective.Add_OnStateChange((oldState, newState, isRecall) =>
                        {
                            if (oldState.status != newState.status && newState.status ==  eChainedPuzzleStatus.Solved && !isRecall)
                            {
                                uplinkTerminal.CorruptedUplinkReceiver?.m_command.StartTerminalUplinkSequence(string.Empty, true);
                                ChangeState(uplinkTerminal, new() { status = UplinkStatus.InProgress, currentRoundIndex = 0 });
                            }
                        });
                    }
                    else
                    {
                        uplinkTerminal.m_chainPuzzleForWardenObjective.Add_OnStateChange((oldState, newState, isRecall) =>
                        {
                            if (oldState.status != newState.status && newState.status == eChainedPuzzleStatus.Solved && !isRecall)
                            {
                                uplinkTerminal.m_command.StartTerminalUplinkSequence(uplinkTerminal.UplinkPuzzle.TerminalUplinkIP);
                            }
                        });
                    }
                }
            }

            foreach (var roundOverride in def.RoundOverrides)
            {
                if (roundOverride.ChainedPuzzleToEndRound != 0u)
                {
                    if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(roundOverride.ChainedPuzzleToEndRound, out var block))
                    {
                        EOSLogger.Error($"ChainedPuzzleToEndRound: {roundOverride.ChainedPuzzleToEndRound} specified but didn't find its ChainedPuzzleDatablock definition! Will not build!");
                        continue;
                    }

                    LG_ComputerTerminal t = null!;
                    switch (roundOverride.BuildChainedPuzzleOn)
                    {
                        case UplinkTerminal.SENDER: 
                            t = uplinkTerminal; 
                            break;

                        case UplinkTerminal.RECEIVER: 
                            if(def.SetupAsCorruptedUplink && uplinkTerminal.CorruptedUplinkReceiver != null)
                            {
                                t = uplinkTerminal.CorruptedUplinkReceiver;
                            }
                            else
                            {
                                EOSLogger.Error($"ChainedPuzzleToEndRound: {roundOverride.ChainedPuzzleToEndRound} specified to build on receiver but this is not a properly setup-ed corr-uplink! Will build ChainedPuzzle on sender side");
                                t = uplinkTerminal;
                            }
                            break;

                        default: 
                            EOSLogger.Error($"Unimplemented enum UplinkTerminal type {roundOverride.BuildChainedPuzzleOn}"); 
                            continue;
                    }

                    roundOverride.ChainedPuzzleToEndRoundInstance = ChainedPuzzleManager.CreatePuzzleInstance
                    (
                        block,
                        t.SpawnNode.m_area,
                        t.m_wardenObjectiveSecurityScanAlign.position,
                        t.m_wardenObjectiveSecurityScanAlign
                    );
                    _builtRoundPuzzles.Add(roundOverride);
                }
            }

            SetupUplinkReplicator(uplinkTerminal);
            EOSLogger.Debug($"BuildUplink: built on {(def.DimensionIndex, def.LayerType, def.LocalIndex, def.InstanceIndex)}");
        }
        
        private void SetupUplinkPuzzle(TerminalUplinkPuzzle uplinkPuzzle, LG_ComputerTerminal terminal, UplinkDefinition def)
        {
            uplinkPuzzle.m_rounds = new List<TerminalUplinkPuzzleRound>().ToIl2Cpp();
            uplinkPuzzle.TerminalUplinkIP = SerialGenerator.GetIpAddress();
            uplinkPuzzle.m_roundIndex = 0;
            uplinkPuzzle.m_lastRoundIndexToUpdateGui = -1;
            uplinkPuzzle.m_position = terminal.transform.position;
            uplinkPuzzle.IsCorrupted = def.SetupAsCorruptedUplink && terminal.CorruptedUplinkReceiver != null;
            uplinkPuzzle.m_terminal = terminal;
            uint verificationRound = Math.Max(def.NumberOfVerificationRounds, 1u);

            for (int i = 0; i < verificationRound; ++i)
            {
                int candidateWords = 6;
                TerminalUplinkPuzzleRound uplinkPuzzleRound = new()
                {
                    CorrectIndex = Builder.SessionSeedRandom.Range(0, candidateWords, "NO_TAG"),
                    Prefixes = new string[candidateWords],
                    Codes = new string[candidateWords]
                };

                for (int j = 0; j < candidateWords; ++j)
                {
                    uplinkPuzzleRound.Codes[j] = SerialGenerator.GetCodeWord();
                    uplinkPuzzleRound.Prefixes[j] = SerialGenerator.GetCodeWordPrefix();
                }

                uplinkPuzzle.m_rounds.Add(uplinkPuzzleRound);
            }
        }

        private void SetupUplinkReplicator(LG_ComputerTerminal uplinkTerminal)
        {
            uint replicatorID = EOSNetworking.AllotReplicatorID();
            if (replicatorID == EOSNetworking.INVALID_ID)
            {
                EOSLogger.Error($"BuildUplink: Cannot create state replicator!");
                return;
            }

            var replicator = StateReplicator<UplinkState>.Create(replicatorID, new() { status = UplinkStatus.Unfinished }, LifeTimeType.Session);
            replicator!.OnStateChanged += (oldState, newState, isRecall) =>
            {
                if (oldState.status == newState.status) return;
                EOSLogger.Log($"Uplink - OnStateChanged: {oldState.status} -> {newState.status}");
                switch (newState.status)
                {
                    case UplinkStatus.Unfinished:
                        uplinkTerminal.UplinkPuzzle.CurrentRound.ShowGui = false;
                        uplinkTerminal.UplinkPuzzle.Connected = false;
                        uplinkTerminal.UplinkPuzzle.Solved = false;
                        uplinkTerminal.UplinkPuzzle.m_roundIndex = 0;
                        break;
                    case UplinkStatus.InProgress:
                        uplinkTerminal.UplinkPuzzle.CurrentRound.ShowGui = true;
                        uplinkTerminal.UplinkPuzzle.Connected = true;
                        uplinkTerminal.UplinkPuzzle.Solved = false;
                        uplinkTerminal.UplinkPuzzle.m_roundIndex = newState.currentRoundIndex;
                        break;
                    case UplinkStatus.Finished:
                        uplinkTerminal.UplinkPuzzle.CurrentRound.ShowGui = false;
                        uplinkTerminal.UplinkPuzzle.Connected = true;
                        uplinkTerminal.UplinkPuzzle.Solved = true;
                        uplinkTerminal.UplinkPuzzle.m_roundIndex = uplinkTerminal.UplinkPuzzle.m_rounds.Count - 1;
                        break;
                }
            };

            _stateReplicators[uplinkTerminal.Pointer] = replicator;
            EOSLogger.Debug($"BuildUplink: Replicator created");
        }

        internal void ChangeState(LG_ComputerTerminal terminal, UplinkState newState)
        {
            if (!_stateReplicators.ContainsKey(terminal.Pointer))
            {
                EOSLogger.Error($"{terminal.ItemKey} doesn't have a registered StateReplicator!");
                return;
            }

            if(SNet.IsMaster)
            {
                _stateReplicators[terminal.Pointer].SetState(newState);
            }
        }
    }
}