using AmorLib.Utils.JsonElementConverters;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using LevelGeneration;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Objectives.Reactor
{
    internal sealed class ReactorStartupOverrideManager : InstanceDefinitionManager<ReactorStartupOverride, ReactorStartupOverrideManager>
    {
        public enum ReactorEventType
        {
            ReactorStartup = 150,
            CompleteCurrentVerify = 151,
        }

        protected override string DEFINITION_NAME => "ReactorStartup";
        public override uint ChainedPuzzleLoadOrder => 5u;

        public static LocaleText MainTerminalText { get; private set; } = LocaleText.Empty;
        public static LocaleText SpecialCmdVerifyText { get; private set; } = LocaleText.Empty;
        public static LocaleText CooldownCommandDesc { get; private set; } = LocaleText.Empty;
        public static LocaleText InfiniteWaveVerifyText { get; private set; } = LocaleText.Empty;
        public static LocaleText NotReadyForVerificationOutputText { get; private set; } = LocaleText.Empty;
        public static LocaleText IncorrectTerminalOutputText { get; private set; } = LocaleText.Empty;
        public static LocaleText CorrectTerminalOutputText { get; private set; } = LocaleText.Empty;

        static ReactorStartupOverrideManager()
        {
            EOSWardenEventManager.AddEventDefinition(ReactorEventType.ReactorStartup.ToString(), (uint)ReactorEventType.ReactorStartup, ReactorStartup);
            EOSWardenEventManager.AddEventDefinition(ReactorEventType.CompleteCurrentVerify.ToString(), (uint)ReactorEventType.CompleteCurrentVerify, CompleteCurrentVerify);
        }

        protected override void AddDefinitions(InstanceDefinitionsForLevel<ReactorStartupOverride> definitions)
        {
            definitions.Definitions.ForEach(def => def.Overrides.Sort((o1, o2) => o1.WaveIndex.CompareTo(o2.WaveIndex)));
            base.AddDefinitions(definitions);
        }

        public bool TryGetDefinition(LG_WardenObjective_Reactor reactor, [MaybeNullWhen(false)] out ReactorStartupOverride definition)
        {
            var (globalIndex, instanceIndex) = ReactorInstanceManager.Current.GetGlobalInstance(reactor);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }

        protected override void OnEnterLevel() // FetchOverrideTextDB
        {
            MainTerminalText = new()
            {
                ID = TextDataBlock.GetBlockID("InGame.WardenObjective_Reactor.MeltdownMainTerminalName"),
                RawText = "Main Terminal"
            };
            SpecialCmdVerifyText = new()
            {
                ID = TextDataBlock.GetBlockID("InGame.WardenObjective_Reactor.MeltdownVerification"),
                RawText = "\"REACTOR COOLING REQUIRED ({0}/{1})\\nMANUAL OVERRIDE REQUIRED. USE COMMAND <color=orange>REACTOR_COOLDOWN</color> AT {2}"
            };
            CooldownCommandDesc = new()
            {
                ID = TextDataBlock.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CommandDesc"),
                RawText = "Confirm Reactor Startup Cooling Protocol"
            };            
            InfiniteWaveVerifyText = new()
            {
                ID = TextDataBlock.GetBlockID("InGame.WardenObjective_Reactor.Verification.InfiniteWave"),
                RawText = "VERIFICATION ({0}/{1})."
            };
            NotReadyForVerificationOutputText = new()
            {
                ID = TextDataBlock.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.Not_ReadyForVerification_Output"),
                RawText = "<color=red>Reactor intensive test in progress, cannot initate cooldown</color>"
            };
            IncorrectTerminalOutputText = new()
            {
                ID = TextDataBlock.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.IncorrectTerminal_Output"),
                RawText = "<color=red>Reactor stage cooldown completed</color>"
            };
            CorrectTerminalOutputText = new(TextDataBlock.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CorrectTerminal_Output"));
        }
        
        internal static void Build(LG_WardenObjective_Reactor reactor, ReactorStartupOverride def)
        {
            var overrideReactorComp = reactor.gameObject.AddComponent<OverrideReactorComp>();
            overrideReactorComp.Init(reactor, def);

            ReactorInstanceManager.Current.MarkAsStartupReactor(reactor);
            ReactorInstanceManager.SetupReactorTerminal(reactor, def.ReactorTerminal);

            def.ChainedPuzzleToActiveInstance = reactor.m_chainedPuzzleToStartSequence;
            EOSLogger.Debug($"ReactorStartup: {def}, override completed");
        }

        private static void ReactorStartup(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster) 
                return;

            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(e.Layer, out var data) || data == null)
            {
                EOSLogger.Error("CompleteCurrentReactorWave: Cannot get WardenObjectiveDataBlock");
                return;
            }
            if (data.Type != eWardenObjectiveType.Reactor_Startup)
            {
                EOSLogger.Error($"CompleteCurrentReactorWave: {e.Layer} is not ReactorStartup. CompleteCurrentReactorWave is invalid.");
                return;
            }
            var reactor = ReactorInstanceManager.FindVanillaReactor(e.Layer, e.Count);
            if (reactor == null)
            {
                EOSLogger.Error($"ReactorStartup: Cannot find reactor in {e.Layer}.");
                return;
            }

            if (reactor.m_currentState.status == eReactorStatus.Inactive_Idle)
            {
                reactor.AttemptInteract(eReactorInteraction.Initiate_startup);
                reactor.m_terminal.TrySyncSetCommandHidden(TERM_Command.ReactorStartup);
                EOSLogger.Debug($"ReactorStartup: Current reactor wave for {e.Layer} completed");
            }            
        }

        private static void CompleteCurrentVerify(WardenObjectiveEventData e)
        {
            if (!WardenObjectiveManager.Current.TryGetActiveWardenObjectiveData(e.Layer, out var data) || data == null)
            {
                EOSLogger.Error("CompleteCurrentReactorWave: Cannot get WardenObjectiveDataBlock");
                return;
            }
            if (data.Type != eWardenObjectiveType.Reactor_Startup)
            {
                EOSLogger.Error($"CompleteCurrentReactorWave: {e.Layer} is not ReactorStartup. CompleteCurrentReactorWave is invalid.");
                return;
            }

            var reactor = ReactorInstanceManager.FindVanillaReactor(e.Layer, e.Count);
            if (reactor == null)
            {
                EOSLogger.Error($"CompleteCurrentReactorWave: Cannot find reactor in {e.Layer}.");
                return;
            }

            if (SNet.IsMaster)
            {
                if (reactor.m_currentWaveCount == reactor.m_waveCountMax)
                    reactor.AttemptInteract(eReactorInteraction.Finish_startup);
                else
                    reactor.AttemptInteract(eReactorInteraction.Verify_startup);
            }
            else // execute OnEnd event on client side 
            {
                WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(reactor.m_currentWaveData.Events, eWardenObjectiveEventTrigger.OnEnd, false);
            }

            EOSLogger.Debug($"CompleteCurrentReactorWave: Current reactor verify for {e.Layer} completed");
        }
    }
}
