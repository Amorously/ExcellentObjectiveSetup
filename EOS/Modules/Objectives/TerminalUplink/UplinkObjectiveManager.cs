using AmorLib.Networking.StateReplicators;
using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using AmorLib.Utils.JsonElementConverters;
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
    internal sealed class UplinkObjectiveManager: InstanceDefinitionManager<UplinkDefinition, UplinkObjectiveManager>
    {
        protected override string DEFINITION_NAME { get; } = "TerminalUplink";
        public override uint ChainedPuzzleLoadOrder => 3u;

        public static LocaleText UplinkAddrLogContent { get; private set; } = LocaleText.Empty;

        private static System.Random _random = null!;
        private readonly List<UplinkRound> _builtRoundPuzzles = new();
        private readonly Dictionary<IntPtr, StateReplicator<UplinkState>?> _stateReplicators = new();        

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

        protected override void OnBuildDone()
        {
            if (!InstanceDefinitions.ContainsKey(CurrentMainLevelLayout)) 
                return;

            UplinkAddrLogContent = new()
            {
                ID = TextDataBlock.GetBlockID("InGame.UplinkTerminal.UplinkAddrLog"),
                RawText = "Available uplink address for TERMINAL_{0}: {1}"
            };
            _random = new(RundownManager.GetActiveExpeditionData().sessionSeed);

            foreach (var def in GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                Build(def);
            }
        }

        protected override void OnEnterLevel()
        {
            foreach (var term in _stateReplicators.Select(kvp => new LG_ComputerTerminal(kvp.Key)))
            {
                RerollUplinkRounds(term.UplinkPuzzle, CheckpointManager.CheckpointUsage);
            }
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _builtRoundPuzzles.ForEach(r => { r.ChainedPuzzleToEndRoundInstance = null!; });
            _builtRoundPuzzles.Clear();
            _stateReplicators.ForEachValue(u => u?.Unload());
            _stateReplicators.Clear();
        }

        private void Build(UplinkDefinition def)
        {
            if (!TerminalInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var uplinkTerminal))
            {
                EOSLogger.Error($"BuildUplink: terminal {def} does not exist!");
                return;
            }

            if (uplinkTerminal.m_isWardenObjective && uplinkTerminal.UplinkPuzzle != null)
            {
                EOSLogger.Error($"BuildUplink: terminal uplink {def} already built (by vanilla or custom build), aborting!");
                return;
            }

            if (def.SetupAsCorruptedUplink)
            {
                if (!TerminalInstanceManager.Current.TryGetInstanceFromUplinkDef(def.CorruptedUplinkReceiver, out var receiver))
                {
                    EOSLogger.Error($"BuildUplink: SetupAsCorruptedUplink specified but didn't find the receiver terminal! Aborting... sender was: {def}");
                    return;
                }

                if (receiver.Pointer == uplinkTerminal.Pointer)
                {
                    EOSLogger.Error($"BuildUplink: don't specify uplink sender and receiver on the same terminal {def}");
                    return;
                }

                uplinkTerminal.CorruptedUplinkReceiver = receiver;
                receiver.CorruptedUplinkReceiver = uplinkTerminal; // need to set on both side
            }

            uplinkTerminal.UplinkPuzzle = new();
            SetupUplinkPuzzle(uplinkTerminal, def);
            uplinkTerminal.UplinkPuzzle.OnPuzzleSolved += new Action(() => EOSWardenEventManager.ExecuteWardenEvents(def.EventsOnComplete));

            uplinkTerminal.m_command.AddCommand
            (
                !def.SetupAsCorruptedUplink || uplinkTerminal.CorruptedUplinkReceiver == null ? TERM_Command.TerminalUplinkConnect : TERM_Command.TerminalCorruptedUplinkConnect, 
                def.UseUplinkAddress ? "UPLINK_CONNECT" : "UPLINK_ESTABLISH", 
                new LocalizedText() { UntranslatedText = Text.Get(3914968919), Id = 3914968919 }
            );

            uplinkTerminal.m_command.AddCommand(TERM_Command.TerminalUplinkVerify, "UPLINK_VERIFY", new LocalizedText() { UntranslatedText = Text.Get(1728022075), Id = 1728022075 });

            if (def.UseUplinkAddress)
            {
                if (!TerminalInstanceManager.Current.TryGetInstanceFromUplinkDef(def.UplinkAddressLogPosition, out var addressLogTerminal))
                {
                    EOSLogger.Error("BuildUplinkOverride: didn't find the uplink address log terminal, will put on uplink terminal");
                    addressLogTerminal = uplinkTerminal;
                }

                addressLogTerminal.AddLocalLog(new TerminalLogFileData()
                {
                    FileName = $"UPLINK_ADDR_{uplinkTerminal.m_serialNumber}.LOG",
                    FileContent = new LocalizedText() { UntranslatedText = string.Format(UplinkAddrLogContent, uplinkTerminal.m_serialNumber, uplinkTerminal.UplinkPuzzle.TerminalUplinkIP), Id = 0 }
                });

                addressLogTerminal.m_command.ClearOutputQueueAndScreenBuffer();
                addressLogTerminal.m_command.AddInitialTerminalOutput();
            }

            if (def.ChainedPuzzleToStartUplink != 0)
            {                
                if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(def.ChainedPuzzleToStartUplink, out var block))
                {
                    EOSLogger.Error($"BuildTerminalUplink: ChainedPuzzleToStartUplink with id {def.ChainedPuzzleToStartUplink} is specified, but no enabled ChainedPuzzleDataBlock definition was found...");
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

                    bool corrupted = def.SetupAsCorruptedUplink && uplinkTerminal.CorruptedUplinkReceiver != null;
                    uplinkTerminal.m_chainPuzzleForWardenObjective.Add_OnStateChange((oldState, newState, isRecall) =>
                    {
                        if (oldState.status == newState.status || newState.status != eChainedPuzzleStatus.Solved || isRecall)
                            return;

                        if (corrupted)
                            uplinkTerminal.CorruptedUplinkReceiver?.m_command.StartTerminalUplinkSequence(string.Empty, true);                            
                        else
                            uplinkTerminal.m_command.StartTerminalUplinkSequence(uplinkTerminal.UplinkPuzzle.TerminalUplinkIP);
                    });
                }
            }

            foreach (var roundOverride in def.RoundOverrides)
            {
                if (roundOverride.ChainedPuzzleToEndRound == 0u)
                    continue;
                
                if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(roundOverride.ChainedPuzzleToEndRound, out var block))
                {
                    EOSLogger.Error($"ChainedPuzzleToEndRound: {roundOverride.ChainedPuzzleToEndRound} was specified, but didn't find its enabled ChainedPuzzleDatablock definition...");
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
                            EOSLogger.Error($"ChainedPuzzleToEndRound: {roundOverride.ChainedPuzzleToEndRound} specified to build on receiver but this is not a properly setup corr-uplink! Will build ChainedPuzzle on sender side");
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

            SetupUplinkReplicator(uplinkTerminal);
            EOSLogger.Debug($"BuildUplink: built on {def}");
        }

        private static void SetupUplinkPuzzle(LG_ComputerTerminal terminal, UplinkDefinition def)
        {
            var uplinkPuzzle = terminal.UplinkPuzzle;
            uplinkPuzzle.m_rounds = new List<TerminalUplinkPuzzleRound>().ToIl2Cpp();
            uplinkPuzzle.TerminalUplinkIP = SerialGenerator.GetIpAddress();
            uplinkPuzzle.m_roundIndex = 0;
            uplinkPuzzle.m_lastRoundIndexToUpdateGui = -1;
            uplinkPuzzle.m_position = terminal.transform.position;
            uplinkPuzzle.IsCorrupted = def.SetupAsCorruptedUplink && terminal.CorruptedUplinkReceiver != null;
            uplinkPuzzle.m_terminal = terminal;

            uint verificationRounds = Math.Max(def.NumberOfVerificationRounds, 1u);
            int candidateWords = 6;
            for (int i = 0; i < verificationRounds; ++i)
            {
                TerminalUplinkPuzzleRound uplinkPuzzleRound = new()
                {
                    CorrectIndex = _random.Next(0, candidateWords),
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

        private static void RerollUplinkRounds(TerminalUplinkPuzzle uplinkPuzzle, int retryCount)
        {
            for (int i = 0; i < retryCount; i++)
            {
                foreach (var uplinkRound in uplinkPuzzle.m_rounds)
                {
                    uplinkRound.CorrectIndex = _random.Next(0, 6);
                }
            }
        }

        private void SetupUplinkReplicator(LG_ComputerTerminal uplinkTerminal)
        {
            uint allottedID = EOSNetworking.AllotReplicatorID();
            if (allottedID == EOSNetworking.INVALID_ID)
            {
                EOSLogger.Error("BuildUplink: replicator IDs depleted, cannot setup StateReplicator");
                return;
            }

            var replicator = StateReplicator<UplinkState>.Create(allottedID, new() { status = UplinkStatus.Unfinished }, LifeTimeType.Session);
            replicator!.OnStateChanged += (oldState, state, isRecall) =>
            {
                if (oldState.status == state.status) return;
                EOSLogger.Debug($"Uplink Terminal_{uplinkTerminal.m_serialNumber} - OnStateChanged: {oldState.status} -> {state.status}");
                switch (state.status)
                {
                    case UplinkStatus.Unfinished:
                        uplinkTerminal.UplinkPuzzle.CurrentRound.ShowGui = false;
                        uplinkTerminal.UplinkPuzzle.Connected = false;
                        uplinkTerminal.UplinkPuzzle.Solved = false;
                        uplinkTerminal.UplinkPuzzle.m_roundIndex = 0;
                        if (isRecall)
                        {
                            RerollUplinkRounds(uplinkTerminal.UplinkPuzzle, CheckpointManager.CheckpointUsage - oldState.retryCount);
                        }
                        break;

                    case UplinkStatus.InProgress:
                        uplinkTerminal.UplinkPuzzle.CurrentRound.ShowGui = true;
                        uplinkTerminal.UplinkPuzzle.Connected = true;
                        uplinkTerminal.UplinkPuzzle.Solved = false;
                        uplinkTerminal.UplinkPuzzle.m_roundIndex = state.currentRoundIndex;
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
        }

        internal void ChangeState(LG_ComputerTerminal terminal, UplinkState newState, bool clientCanSend = false)
        {
            if (!_stateReplicators.TryGetValue(terminal.Pointer, out var replicator))
            {
                EOSLogger.Error($"{terminal.ItemKey} doesn't have a registered StateReplicator!");
                return;
            }

            if (SNet.IsMaster || clientCanSend)
            {
                replicator?.SetState(newState with
                {
                    firstRoundOutputted = true,
                    retryCount = CheckpointManager.CheckpointUsage
                });
            }
        }

        internal bool FirstRoundOutputted(LG_ComputerTerminal terminal)
        {
            if (!_stateReplicators.TryGetValue(terminal.Pointer, out var replicator))
            {
                EOSLogger.Error($"{terminal.ItemKey} doesn't have a registered StateReplicator!");
                return false;
            }

            return replicator?.State.firstRoundOutputted ?? false;
        }
    }
}
