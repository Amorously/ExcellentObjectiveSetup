using AK;
using AmorLib.Utils;
using AmorLib.Utils.Extensions;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;
using Localization;
using SNetwork;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Objectives.Reactor
{
    internal sealed class ReactorShutdownObjectiveManager : InstanceDefinitionManager<ReactorShutdownDefinition, ReactorShutdownObjectiveManager>
    {
        protected override string DEFINITION_NAME => "ReactorShutdown";
        public override uint ChainedPuzzleLoadOrder => 4u;

        public bool TryGetDefinition(LG_WardenObjective_Reactor reactor, [MaybeNullWhen(false)] out ReactorShutdownDefinition definition)
        {
            var (globalIndex, instanceIndex) = ReactorInstanceManager.Current.GetGlobalInstance(reactor);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }

        internal static void Build(LG_WardenObjective_Reactor reactor, ReactorShutdownDefinition def)
        {
            if (reactor.m_isWardenObjective)
            {
                EOSLogger.Error($"ReactorShutdown: Reactor definition for reactor {def} is already setup by vanilla, won't build.");
                return;
            }

            GenericObjectiveSetup(reactor, def);

            reactor.m_lightCollection = LG_LightCollection.Create(reactor.m_reactorArea.m_courseNode, reactor.m_terminalAlign.position, LG_LightCollectionSorting.Distance);
            reactor.m_lightCollection.SetMode(true);

            if (def.PutVerificationCodeOnTerminal)
            {
                var verifyTerminal = TerminalInstanceManager.Current.GetInstance(def.VerificationCodeTerminal.IntTuple, def.VerificationCodeTerminal.InstanceIndex);
                if (verifyTerminal == null)
                {
                    EOSLogger.Error($"ReactorShutdown: PutVerificationCodeOnTerminal is specified but could NOT find terminal {def.VerificationCodeTerminal}, will show verification code upon shutdown initiation");
                }
                else
                {
                    string verificationTerminalFileName = "reactor_ver" + SerialGenerator.GetCodeWordPrefix() + ".log";
                    TerminalLogFileData data = new()
                    {
                        FileName = verificationTerminalFileName,
                        FileContent = new LocalizedText()
                        {
                            UntranslatedText = string.Format(Text.Get(182408469), reactor.m_overrideCodes[0].ToUpperInvariant()),
                            Id = 0
                        }
                    };
                    verifyTerminal.AddLocalLog(data, true);
                    verifyTerminal.m_command.ClearOutputQueueAndScreenBuffer();
                    verifyTerminal.m_command.AddInitialTerminalOutput();
                }
            }

            if (reactor.SpawnNode != null && reactor.m_terminalItem != null)
            {
                reactor.m_terminalItem.SpawnNode = reactor.SpawnNode;
                reactor.m_terminalItem.FloorItemLocation = reactor.SpawnNode.m_zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);
            }

            // build chained puzzle to active 
            if (BuildChainedPuzzle(def.ChainedPuzzleToActive, eReactorInteraction.Initiate_shutdown, out var cp))
            {
                def.ChainedPuzzleToActiveInstance = cp;
            }
            else
            {
                EOSLogger.Debug("ReactorShutdown: Reactor has no ChainedPuzzleToActive, will start shutdown sequence on shutdown command initiation.");
            }

            // build mid obj chained puzzle
            if (BuildChainedPuzzle(def.ChainedPuzzleOnVerification, eReactorInteraction.Finish_shutdown, out var cp2))
            {
                def.ChainedPuzzleOnVerificationInstance = cp2;
            }
            else
            {
                EOSLogger.Debug($"ReactorShutdown: ChainedPuzzleOnVerification unspecified, will complete shutdown on verification.");
            }

            if (reactor.m_terminal?.gameObject.TryAndGetComponent<iLG_SpawnedInNodeHandler>(out var component) == true)
            {
                component.SpawnNode = reactor.SpawnNode;
            }

            reactor.SetLightsEnabled(reactor.m_lightsWhenOff, false);
            reactor.SetLightsEnabled(reactor.m_lightsWhenOn, true);

            ReactorInstanceManager.Current.MarkAsShutdownReactor(reactor);
            EOSLogger.Debug($"ReactorShutdown: {def}, custom setup completed");
        
            bool BuildChainedPuzzle(uint id, eReactorInteraction state, [NotNullWhen(true)] out ChainedPuzzleInstance? cp)
            {
                if (!DataBlockUtil.TryGetBlock<ChainedPuzzleDataBlock>(id, out var block))
                {
                    EOSLogger.Error($"ReactorShutdown: {id} is specified but could not find its ChainedPuzzleDatablock definition!");
                    cp = null;
                    return false;
                }

                cp = ChainedPuzzleManager.CreatePuzzleInstance(block, reactor.SpawnNode?.m_area, reactor.m_chainedPuzzleAlign.position, reactor.transform);
                cp.OnPuzzleSolved += new Action(() =>
                {
                    if (SNet.IsMaster)
                    {
                        reactor.AttemptInteract(state);
                    }
                });
                return cp != null;
            }
        }

        private static void GenericObjectiveSetup(LG_WardenObjective_Reactor reactor, BaseReactorDefinition reactorDefinition)
        {
            reactor.m_stateReplicator = SNet_StateReplicator<pReactorState, pReactorInteraction>.Create(new iSNet_StateReplicatorProvider<pReactorState, pReactorInteraction>(reactor.Pointer), eSNetReplicatorLifeTime.DestroyedOnLevelReset);
            reactor.m_serialNumber = SerialGenerator.GetUniqueSerialNo();
            reactor.m_itemKey = "REACTOR_" + reactor.m_serialNumber.ToString();
            reactor.m_terminalItem = GOUtil.GetInterfaceFromComp<iTerminalItem>(reactor.m_terminalItemComp);
            reactor.m_terminalItem.Setup(reactor.m_itemKey);
            reactor.m_terminalItem.FloorItemStatus = EnumUtil.GetRandomValue<eFloorInventoryObjectStatus>();

            reactor.m_overrideCodes = new string[1] { SerialGenerator.GetCodeWord() };
            //reactor.CurrentStateOverrideCode = reactor.m_overrideCodes[0];

            reactor.m_terminalItem.OnWantDetailedInfo = new Func<Il2CppSystem.Collections.Generic.List<string>, Il2CppSystem.Collections.Generic.List<string>>(defaultDetails =>
            {
                List<string> stringList = new()
                {
                    "----------------------------------------------------------------",
                    "MAIN POWER REACTOR"
                };
                foreach (var detail in defaultDetails)
                {
                    stringList.Add(detail);
                }

                stringList.Add("----------------------------------------------------------------");
                return stringList.ToIl2Cpp();
            });
            reactor.m_terminal = GOUtil.SpawnChildAndGetComp<LG_ComputerTerminal>(reactor.m_terminalPrefab, reactor.m_terminalAlign);
            reactor.m_terminal.Setup();
            reactor.m_terminal.ConnectedReactor = reactor;

            ReactorInstanceManager.SetupReactorTerminal(reactor, reactorDefinition.ReactorTerminal);

            reactor.m_sound = new(reactor.m_terminalAlign.position);
            reactor.m_sound.Post(EVENTS.REACTOR_POWER_LEVEL_1_LOOP);
            reactor.m_sound.SetRTPCValue(GAME_PARAMETERS.REACTOR_POWER, 100f);
            reactor.m_terminal.m_command.SetupReactorCommands(false, true);
        }
    }
}
