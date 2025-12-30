using AmorLib.Utils;
using EOS.Modules.Instances;
using GameData;
using GTFO.API;
using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using Localization;
using SNetwork;
using UnityEngine;

namespace EOS.Modules.Objectives.Reactor
{
    public class OverrideReactorComp : MonoBehaviour
    {
        public LG_WardenObjective_Reactor ChainedReactor { get; internal set; } = null!;
        [HideFromIl2Cpp]
        public ReactorStartupOverride OverrideData { get; internal set; } = null!;

        private readonly List<WaveOverride> _waveData = new();

        public WardenObjectiveDataBlock ObjectiveData => OverrideData?.ObjectiveDB!;

        private (int, int, int) OrigVerifyZone(eLocalZoneIndex zoneForVerification)
        {
            return GlobalIndexUtil.ToIntTuple(ChainedReactor.SpawnNode.m_dimension.DimensionIndex, ChainedReactor.SpawnNode.LayerType, zoneForVerification);
        }

        [HideFromIl2Cpp]
        public void Init(LG_WardenObjective_Reactor reactor, ReactorStartupOverride def)
        {
            ChainedReactor = reactor;
            OverrideData = def;

            for (int j = 0; j < OverrideData.Overrides.Count; j++)
            {
                var overrideWave = OverrideData.Overrides[j];
                var prevOverride = j - 1 >= 0 ? OverrideData.Overrides[j - 1] : null;
                if (prevOverride != null && overrideWave.WaveIndex == prevOverride.WaveIndex)
                {
                    EOSLogger.Error($"Found duplicate wave index {overrideWave.WaveIndex}, this could lead to reactor override exception!");
                    continue;
                }

                for (int i = prevOverride?.WaveIndex + 1 ?? 0; i < overrideWave.WaveIndex; i++)
                    _waveData.Add(new() { WaveIndex = i });
                _waveData.Add(overrideWave);
            }

            _waveData.ForEach(w => w.CustomVerifyText = w.VerifySequenceText.ParseTextFragments());
            LevelAPI.OnEnterLevel += OnEnterLevel;
        }

        private void OnEnterLevel()
        {
            if (ObjectiveData.Type != eWardenObjectiveType.Reactor_Startup)
            {
                EOSLogger.Error("Only reactor startup is supported");
                enabled = false;
                return;
            }

            if (OverrideData.StartupOnDrop)
            {
                if (SNet.IsMaster)
                {
                    ChainedReactor.AttemptInteract(eReactorInteraction.Initiate_startup);
                    ChainedReactor.m_terminal.TrySyncSetCommandHidden(TERM_Command.ReactorStartup);
                }
            }

            // Note: VerifyZoneOverride first, then Meltdown and infinite wave setup.
            //       If a wave's VerifyZone is overriden, then we can find the overriden target terminal in our dictionary when setting up meltdown wave.
            //       Otherwise (the wave's VerifyZone is not overriden), we will find the terminal when setting up.
            // Both VerifyZoneOverride and MeltdownAndInfiniteWave setup requires finding the original terminal (if exists).
            // By specifying the order of doing the 2 tasks, we won't need to find the original terminal for multiple times.
            SetupVerifyZoneOverrides();
            SetupWaves();
        }

        private void SetupVerifyZoneOverrides()
        {
            foreach (var waveOverride in _waveData)
            {
                if (!waveOverride.ChangeVerifyZone) 
                    continue;
                if (waveOverride.VerificationType == EOSReactorVerificationType.BY_WARDEN_EVENT)
                {
                    EOSLogger.Error($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Verification Type is {EOSReactorVerificationType.BY_WARDEN_EVENT}, which doesn't work with VerifyZoneOverride");
                    continue;
                }

                var verifyIndex = waveOverride.VerifyZone;
                var moveToZone = verifyIndex.Zone;
                if (moveToZone == null)
                {
                    EOSLogger.Error($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Cannot find target zone {verifyIndex}.");
                    continue;
                }
                if (moveToZone.TerminalsSpawnedInZone == null || moveToZone.TerminalsSpawnedInZone.Count == 0)
                {
                    EOSLogger.Error($"VerifyZoneOverrides: No spawned terminal found in target zone {verifyIndex}.");
                    continue;
                }

                // === find target terminal ===
                LG_ComputerTerminal? targetTerminal = null;
                if (verifyIndex.InstanceIndex >= 0)
                {
                    targetTerminal = TerminalInstanceManager.Current.GetInstance(verifyIndex.IntTuple, verifyIndex.InstanceIndex);
                    if (targetTerminal == null)
                    {
                        EOSLogger.Error($"VerifyZoneOverride: cannot find target terminal with Terminal Instance Index: {waveOverride}");
                        continue;
                    }
                }
                else if (TerminalInstanceManager.Current.TryGetInstancesInZone(verifyIndex.IntTuple, out var terminalsInZone))
                {
                    int terminalIndex = Builder.SessionSeedRandom.Range(0, terminalsInZone.Count);
                    targetTerminal = terminalsInZone[terminalIndex];
                }
                else
                {
                    continue;
                }
                waveOverride.VerifyTerminal = targetTerminal;

                // === verify override ===
                if (waveOverride.WaveIndex >= ObjectiveData.ReactorWaves.Count)
                    continue;
                var waveData = ObjectiveData.ReactorWaves[waveOverride.WaveIndex];
                TerminalLogFileData? verifyLog = null;
                if (waveData.VerifyInOtherZone)
                {
                    var origVerTerms = EOSTerminalUtil.FindTerminals(OrigVerifyZone(waveData.ZoneForVerification), x => x.ItemKey == waveData.VerificationTerminalSerial);
                    if (origVerTerms == null || origVerTerms.Count == 0)
                    {
                        EOSLogger.Error($"Wave_{waveOverride.WaveIndex}: cannot find vanilla verification terminal in {OrigVerifyZone(waveData.ZoneForVerification)}, unable to override");
                        continue;
                    }
                    LG_ComputerTerminal? origTerminal = origVerTerms[0];
                    if (origTerminal == null)
                    {
                        EOSLogger.Error($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Cannot find log terminal");
                        continue;
                    }

                    string logName = waveData.VerificationTerminalFileName.ToUpperInvariant();
                    verifyLog = origTerminal.GetLocalLog(logName);
                    if (verifyLog == null)
                    {
                        EOSLogger.Error("VerifyZoneOverrides: Cannot find vanilla-generated reactor verify log on terminal...");
                        continue;
                    }

                    origTerminal.RemoveLocalLog(logName);
                    origTerminal.ResetInitialOutput();
                }
                else
                {
                    waveData.VerificationTerminalFileName = "reactor_ver" + SerialGenerator.GetCodeWordPrefix() + ".log"; 
                    verifyLog = new TerminalLogFileData()
                    {
                        FileName = waveData.VerificationTerminalFileName.ToUpperInvariant(),
                        FileContent = new LocalizedText()
                        {
                            UntranslatedText = string.Format(Text.Get(182408469), ChainedReactor.m_overrideCodes[waveOverride.WaveIndex].ToUpper()),
                            Id = 0
                        }
                    };

                    EOSLogger.Debug($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} - Log generated.");
                }

                waveData.HasVerificationTerminal = true; 
                waveData.VerificationTerminalSerial = targetTerminal.ItemKey; 
                targetTerminal.AddLocalLog(verifyLog, true);
                targetTerminal.ResetInitialOutput();
                EOSLogger.Debug($"VerifyZoneOverrides: Wave_{waveOverride.WaveIndex} verification overriden");
            }
        }

        private void SetupWaves() // meltdown / infinite wave handle
        {
            int num_BY_SPECIAL_COMMAND = 0;
            for (int waveIndex = 0; waveIndex < _waveData.Count; waveIndex++)
            {
                ReactorWaveData reactorWave = ObjectiveData.ReactorWaves[waveIndex];
                WaveOverride waveOverride = _waveData[waveIndex];

                switch (waveOverride.VerificationType)
                {
                    case EOSReactorVerificationType.NORMAL:
                        break;

                    case EOSReactorVerificationType.BY_SPECIAL_COMMAND:
                        if (!reactorWave.HasVerificationTerminal) // verify on reactor
                        {
                            waveOverride.VerifyTerminal = ChainedReactor.m_terminal;
                            AddVerifyCommand(ChainedReactor.m_terminal);
                        }
                        else
                        {
                            LG_ComputerTerminal? targetTerminal = waveOverride.VerifyTerminal;
                            if (targetTerminal == null) // verify zone is not overriden
                            {
                                targetTerminal = EOSTerminalUtil.FindTerminals(OrigVerifyZone(reactorWave.ZoneForVerification), terminal => terminal.ItemKey.Equals(reactorWave.VerificationTerminalSerial, StringComparison.InvariantCultureIgnoreCase))[0];
                                if (targetTerminal == null)
                                {
                                    EOSLogger.Error($"SetupWaves: cannot find verify terminal for Wave_{waveIndex}, skipped");
                                    continue;
                                }
                                waveOverride.VerifyTerminal = targetTerminal;
                            }
                            targetTerminal.ConnectedReactor = ChainedReactor;
                            targetTerminal.RemoveLocalLog(reactorWave.VerificationTerminalFileName.ToUpperInvariant());
                            AddVerifyCommand(targetTerminal);
                            targetTerminal.ResetInitialOutput();
                        }
                        num_BY_SPECIAL_COMMAND += 1;
                        EOSLogger.Debug($"WaveOverride: Setup as Wave Verification {EOSReactorVerificationType.BY_SPECIAL_COMMAND} for Wave_{waveIndex}");
                        break;

                    case EOSReactorVerificationType.BY_WARDEN_EVENT: // nothing to do
                        EOSLogger.Debug($"WaveOverride: Setup as Wave Verification {EOSReactorVerificationType.BY_WARDEN_EVENT} for Wave_{waveIndex}");
                        break;

                    default:
                        EOSLogger.Error($"Unimplemented Verification Type {waveOverride.VerificationType}");
                        break;
                }

            }

            if (num_BY_SPECIAL_COMMAND == ObjectiveData.ReactorWaves.Count)
            {
                ChainedReactor.m_terminal.TrySyncSetCommandHidden(TERM_Command.ReactorVerify);
            }
        }

        private static void AddVerifyCommand(LG_ComputerTerminal terminal)
        {
            var mCommand = terminal.m_command;
            if (mCommand.HasRegisteredCommand(TERM_Command.UniqueCommand5))
            {
                EOSLogger.Warning("TERM_Command.UniqueCommand5 already registered...");
                EOSLogger.Debug("...If this terminal is specified as objective terminal for 2 waves and the number of commands in 'UniqueCommands' on this terminal isn't more than 4, simply ignore this message.");
                return;
            }

            mCommand.AddCommand(TERM_Command.UniqueCommand5, "REACTOR_COOLDOWN", ReactorStartupOverrideManager.CooldownCommandDesc);
            terminal.TrySyncSetCommandRule(TERM_Command.UniqueCommand5, TERM_CommandRule.Normal);
        }

        public bool IsCorrectTerminal(LG_ComputerTerminal terminal)
        {
            int index = ChainedReactor.m_currentWaveCount - 1;
            if (index >= 0)
            {
                EOSLogger.Debug(string.Format("Index: {0}", index));
                EOSLogger.Debug("Comp Terminal Key1: " + terminal.ItemKey);
                EOSLogger.Debug("Comp Terminal Key2: " + (_waveData[index].VerifyTerminal != null ? _waveData[index].VerifyTerminal.ItemKey : "empty"));
                if (_waveData[index].VerifyTerminal.ItemKey != null && _waveData[index].VerifyTerminal.ItemKey.Equals(terminal.ItemKey, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public void SetIdle()
        {
            if (ChainedReactor == null)
                return;

            var newState = new pReactorState()
            {
                status = eReactorStatus.Inactive_Idle,
                stateCount = 0,
                stateProgress = 0.0f,
                verifyFailed = false
            };

            ChainedReactor.m_stateReplicator.State = newState;
        }

        public void OnDestroy()
        {
            LevelAPI.OnEnterLevel -= OnEnterLevel;
            ChainedReactor = null!;
            OverrideData = null!;
        }

        public void LateUpdate()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;
            eReactorStatus status = ChainedReactor.m_currentState.status;
            UpdateGUIText(status);
        }

        private void UpdateGUIText(eReactorStatus status)
        {
            int currentWaveIndex = ChainedReactor.m_currentWaveCount - 1;
            if (currentWaveIndex < 0 || currentWaveIndex >= _waveData.Count) 
                return;

            var waveData = _waveData[currentWaveIndex];
            string text = string.Empty;

            if (waveData.UseCustomVerifyText)
            {
                text = waveData.CustomVerifyText;
            }

            if (status == eReactorStatus.Startup_waitForVerify)
            {
                switch (waveData.VerificationType)
                {
                    case EOSReactorVerificationType.NORMAL:
                        if (ChainedReactor.m_currentWaveData.HasVerificationTerminal)
                        {
                            if (!waveData.UseCustomVerifyText)
                            {
                                text = Text.Get(1103U);
                            }
                            ChainedReactor.SetGUIMessage
                            (
                                true,
                                string.Format(text, ChainedReactor.m_currentWaveCount, ChainedReactor.m_waveCountMax, ("<color=orange>" + ChainedReactor.m_currentWaveData.VerificationTerminalSerial + "</color>")),
                                ePUIMessageStyle.Warning,
                                printTimerInText: !waveData.HideVerificationTimer,
                                timerPrefix: "<size=125%>" + Text.Get(1104U),
                                timerSuffix: "</size>"
                            );
                        }
                        else
                        {
                            if (!waveData.UseCustomVerifyText)
                            {
                                text = Text.Get(1105U);
                            }
                            ChainedReactor.SetGUIMessage
                            (
                                true,
                                string.Format(text, ChainedReactor.m_currentWaveCount, ChainedReactor.m_waveCountMax, ("<color=orange>" + ChainedReactor.CurrentStateOverrideCode + "</color>")),
                                ePUIMessageStyle.Warning, 
                                printTimerInText: !waveData.HideVerificationTimer,
                                timerPrefix: "<size=125%>" + Text.Get(1104U),
                                timerSuffix: "</size>"
                            );
                        }
                        break;

                    case EOSReactorVerificationType.BY_SPECIAL_COMMAND:
                        string str = ChainedReactor.m_currentWaveData.HasVerificationTerminal ? ChainedReactor.m_currentWaveData.VerificationTerminalSerial : ReactorStartupOverrideManager.MainTerminalText;
                        if (!waveData.UseCustomVerifyText)
                        {
                            text = ReactorStartupOverrideManager.SpecialCmdVerifyText;
                        }
                        ChainedReactor.SetGUIMessage
                        (
                            true,
                            string.Format(text, ChainedReactor.m_currentWaveCount, ChainedReactor.m_waveCountMax, ("<color=orange>" + str + "</color>")),
                            ePUIMessageStyle.Warning,
                            printTimerInText: !waveData.HideVerificationTimer,
                            timerPrefix: "<size=125%>" + Text.Get(1104U),
                            timerSuffix: "</size>"
                        );
                        break;

                    case EOSReactorVerificationType.BY_WARDEN_EVENT:
                        if (!waveData.UseCustomVerifyText)
                        {
                            text = ReactorStartupOverrideManager.InfiniteWaveVerifyText;
                        }
                        ChainedReactor.SetGUIMessage
                        (
                            true,
                            string.Format(text, ChainedReactor.m_currentWaveCount, ChainedReactor.m_waveCountMax),
                            ePUIMessageStyle.Warning,
                            printTimerInText: !waveData.HideVerificationTimer,
                            timerPrefix: "<size=125%>" + Text.Get(1104U),
                            timerSuffix: "</size>"
                        );
                        break;
                }
            }
        }
    }
}
