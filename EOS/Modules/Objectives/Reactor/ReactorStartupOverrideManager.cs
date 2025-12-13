using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using GTFO.API;
using LevelGeneration;
using Localization;

namespace EOS.Modules.Objectives.Reactor
{
    internal sealed class ReactorStartupOverrideManager : InstanceDefinitionManager<ReactorStartupOverride>
    {
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

        public static ReactorStartupOverrideManager Current { get; private set; } = new();

        private readonly List<ReactorStartupOverride> _builtOverride = new();

        protected override string DEFINITION_NAME => "ReactorStartup";

        public ReactorStartupOverrideManager() // Reactor Build is done in the postfix patch LG_WardenObjective_Reactor.OnBuildDone, instead of in LevelAPI.OnBuildDone
        {
            EventAPI.OnExpeditionStarted += FetchOverrideTextDB;
            EOSWardenEventManager.AddEventDefinition(WardenEvents.EventType.ReactorStartup.ToString(), (uint)WardenEvents.EventType.ReactorStartup, WardenEvents.ReactorStartup);
            EOSWardenEventManager.AddEventDefinition(WardenEvents.EventType.CompleteCurrentVerify.ToString(), (uint)WardenEvents.EventType.CompleteCurrentVerify, WardenEvents.CompleteCurrentVerify);
        }

        public static void FetchOverrideTextDB()
        {
            SpecialCmdVerifyTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownVerification");
            MainTerminalTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownMainTerminalName");
            CooldownCommandDescTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CommandDesc");
            InfiniteWaveVerifyTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.Verification.InfiniteWave");
            NotReadyForVerificationOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.Not_ReadyForVerification_Output");
            IncorrectTerminalOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.IncorrectTerminal_Output");
            CorrectTerminalOutputTextID = GameDataBlockBase<TextDataBlock>.GetBlockID("InGame.WardenObjective_Reactor.MeltdownCoolDown.CorrectTerminal_Output");
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _builtOverride.ForEach(def => { def.ChainedPuzzleToActiveInstance = null!; });
            _builtOverride.Clear();
        }

        protected override void AddDefinitions(InstanceDefinitionsForLevel<ReactorStartupOverride> definitions)
        {
            definitions.Definitions.ForEach(def => def.Overrides.Sort((o1, o2) => o1.WaveIndex.CompareTo(o2.WaveIndex)));
            base.AddDefinitions(definitions);
        }

        internal void Build(LG_WardenObjective_Reactor reactor, ReactorStartupOverride def)
        {
            if (!reactor.m_isWardenObjective)
            {
                EOSLogger.Error($"ReactorStartup: Reactor Override for reactor {def.GlobalZoneIndexTuple()}, Instance_{def.InstanceIndex} is not setup by vanilla, won't override");
                return;
            }

            OverrideReactorComp overrideReactorComp = reactor.gameObject.AddComponent<OverrideReactorComp>();
            overrideReactorComp.Init(reactor, def);

            ReactorInstanceManager.Current.MarkAsStartupReactor(reactor);
            ReactorInstanceManager.Current.SetupReactorTerminal(reactor, def.ReactorTerminal);

            def.ChainedPuzzleToActiveInstance = reactor.m_chainedPuzzleToStartSequence;
            _builtOverride.Add(def);
            EOSLogger.Debug($"ReactorStartup: {def.GlobalZoneIndexTuple()}, Instance_{def.InstanceIndex}, override completed");
        }
    }
}
