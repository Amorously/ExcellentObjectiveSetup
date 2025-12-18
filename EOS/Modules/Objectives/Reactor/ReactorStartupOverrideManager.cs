using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using LevelGeneration;
using Localization;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Objectives.Reactor
{
    internal sealed class ReactorStartupOverrideManager : InstanceDefinitionManager<ReactorStartupOverride, ReactorStartupOverrideManager>
    {
        public enum EventType
        {
            ReactorStartup = 150,
            CompleteCurrentVerify = 151,
        }

        public static uint SpecialCmdVerifyTextID { private set; get; } = 0;
        public static uint MainTerminalTextID { private set; get; } = 0;
        public static uint CooldownCommandDescTextID { private set; get; } = 0;
        public static uint InfiniteWaveVerifyTextID { private set; get; } = 0;
        public static uint NotReadyForVerificationOutputTextID { private set; get; } = 0;
        public static uint IncorrectTerminalOutputTextID { private set; get; } = 0;
        public static uint CorrectTerminalOutputTextID { private set; get; } = 0;
        public static string CoolDownCommandDesc => CooldownCommandDescTextID != 0 ? Text.Get(CooldownCommandDescTextID) : "Confirm Reactor Startup Cooling Protocol";
        public static string MainTerminalText => MainTerminalTextID != 0 ? Text.Get(MainTerminalTextID) : "Main Terminal";
        public static string SpecialCmdVerifyText => SpecialCmdVerifyTextID != 0 ? Text.Get(SpecialCmdVerifyTextID) : "REACTOR COOLING REQUIRED ({0}/{1})\nMANUAL OVERRIDE REQUIRED. USE COMMAND <color=orange>REACTOR_COOLDOWN</color> AT {2}";
        public static string InfiniteWaveVerifyText => InfiniteWaveVerifyTextID != 0 ? Text.Get(InfiniteWaveVerifyTextID) : "VERIFICATION ({0}/{1}).";
        public static string NotReadyForVerificationOutputText => NotReadyForVerificationOutputTextID != 0 ? Text.Get(NotReadyForVerificationOutputTextID) : "<color=red>Reactor intensive test in progress, cannot initate cooldown</color>";
        public static string CorrectTerminalOutputText => CorrectTerminalOutputTextID != 0 ? Text.Get(CorrectTerminalOutputTextID) : "<color=red>Reactor stage cooldown completed</color>";
        public static string IncorrectTerminalOutputText => IncorrectTerminalOutputTextID != 0 ? Text.Get(IncorrectTerminalOutputTextID) : "<color=red>Incorrect terminal, cannot initate cooldown</color>";

        protected override string DEFINITION_NAME => "ReactorStartup";
        public override uint ChainedPuzzleLoadOrder => 5u;

        static ReactorStartupOverrideManager()
        {
            EOSWardenEventManager.AddEventDefinition(EventType.ReactorStartup.ToString(), (uint)EventType.ReactorStartup, ReactorStartup);
            EOSWardenEventManager.AddEventDefinition(EventType.CompleteCurrentVerify.ToString(), (uint)EventType.CompleteCurrentVerify, CompleteCurrentVerify);
        }

        protected override void OnExpeditionStarted() // FetchOverrideTextDB
        {
            SpecialCmdVerifyTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownVerification");
            MainTerminalTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownMainTerminalName");
            CooldownCommandDescTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CommandDesc");
            InfiniteWaveVerifyTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.Verification.InfiniteWave");
            NotReadyForVerificationOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.Not_ReadyForVerification_Output");
            IncorrectTerminalOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.IncorrectTerminal_Output");
            CorrectTerminalOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CorrectTerminal_Output");
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
