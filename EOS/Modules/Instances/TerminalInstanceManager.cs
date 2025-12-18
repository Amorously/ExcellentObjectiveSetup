using AmorLib.Utils;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Objectives.TerminalUplink;
using EOS.Modules.Tweaks.TerminalPosition;
using EOS.Modules.Tweaks.TerminalTweak;
using GameData;
using LevelGeneration;
using SNetwork;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Instances
{
    public sealed class TerminalInstanceManager: InstanceManager<LG_ComputerTerminal, TerminalInstanceManager>
    {
        public enum TerminalWardenEvents 
        { 
            EOSSetTerminalCommand = 600,
            EOSToggleTerminalState = 601,
        }

        public static ImmutableList<TERM_Command> UNIQUE_CMDS { get; } = ImmutableList.Create
        (
            TERM_Command.UniqueCommand1,
            TERM_Command.UniqueCommand2,
            TERM_Command.UniqueCommand3,
            TERM_Command.UniqueCommand4,
            TERM_Command.UniqueCommand5
        );

        private readonly  Dictionary<IntPtr, LG_ComputerTerminal> _uniqueCommandChainPuzzles = new(); // key: ChainedPuzzleInstance.Pointer
        private readonly Dictionary<IntPtr, TerminalWrapper> _terminalWrappers = new();

        static TerminalInstanceManager()
        {
            EOSWardenEventManager.AddEventDefinition(TerminalWardenEvents.EOSSetTerminalCommand.ToString(), (uint)TerminalWardenEvents.EOSSetTerminalCommand, SetTerminalCommand);
            EOSWardenEventManager.AddEventDefinition(TerminalWardenEvents.EOSToggleTerminalState.ToString(), (uint)TerminalWardenEvents.EOSToggleTerminalState, ToggleTerminalState);
        }

        protected override void OnAfterBuildBatch(LG_Factory.BatchName batch)
        {
            if (batch != LG_Factory.BatchName.SpecificSpawning)
                return;

            foreach (var term in Index2Instance.SelectMany(kvp => kvp.Value))
            {
                TerminalPositionOverrideManager.Current.Setup(term);
            }
        }

        protected override void OnBuildDone() // GatherUniqueCommandChainPuzzles
        {
            foreach (var term in Index2Instance.SelectMany(kvp => kvp.Value))
            {
                foreach (var cmd in UNIQUE_CMDS)
                {
                    if (!term.m_command.m_commandsPerEnum.ContainsKey(cmd)) continue;
                    var cmdName = term.m_command.m_commandsPerEnum[cmd];
                    var events = term.GetUniqueCommandEvents(cmdName);

                    for (int i = 0; i < events.Count; i++)
                    {
                        if (term.TryGetChainPuzzleForCommand(cmd, i, out var cp))
                        {
                            _uniqueCommandChainPuzzles[cp.Pointer] = term;
                        }
                    }
                }
            }
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _uniqueCommandChainPuzzles.Clear();
            _terminalWrappers.Clear();
            base.OnLevelCleanup();
        }

        public override (int, int, int) GetGlobalIndex(LG_ComputerTerminal instance)
        {
            if(instance.SpawnNode == null)
            {
                if(instance.ConnectedReactor != null)
                {
                    return instance.ConnectedReactor.SpawnNode.m_zone.ToIntTuple();
                }
                else
                {
                    EOSLogger.Error("LG_ComputerTerminal: both SpawnNode and ConnectedReactor are null!");
                    return (-1, -1, -1);
                }
            }

            return instance.SpawnNode.m_zone.ToIntTuple();
        }

        public override uint Register(LG_ComputerTerminal instance)
        {
            if (instance.SpawnNode == null)
            {
                return INVALID_INSTANCE_INDEX;
            }
            else
            {
                uint index = Register(GetGlobalIndex(instance), instance);
                SetupTerminalWrapper(instance);
                return index;
            }
        }

        public void SetupTerminalWrapper(LG_ComputerTerminal terminal)
        {
            if (_terminalWrappers.ContainsKey(terminal.Pointer))
            {
                EOSLogger.Error($"TerminalInstanceManager: {terminal.ItemKey} is already setup with wrapper...");
                return;
            }

            uint allotedID = EOSNetworking.AllotReplicatorID();
            if (allotedID == EOSNetworking.INVALID_ID)
            {
                EOSLogger.Error($"TerminalStateManager: replicator ID depleted, cannot setup terminal...");
                return;
            }

            TerminalWrapper t = new(terminal, allotedID);
            _terminalWrappers[terminal.Pointer] = t;
        }

        public TerminalWrapper? GetTerminalWrapper(LG_ComputerTerminal terminal) => _terminalWrappers.TryGetValue(terminal.Pointer, out var wrapper) ? wrapper : null;

        public bool TryGetParentTerminal(ChainedPuzzleInstance cpInstance, [MaybeNullWhen(false)] out LG_ComputerTerminal terminal) => _uniqueCommandChainPuzzles.TryGetValue(cpInstance.Pointer, out terminal);

        public bool TryGetParentTerminal(IntPtr pointer, [MaybeNullWhen(false)] out LG_ComputerTerminal terminal) => _uniqueCommandChainPuzzles.TryGetValue(pointer, out terminal);

        public bool TryGetInstanceFromUplinkDef(Terminal term, [MaybeNullWhen(false)] out LG_ComputerTerminal instance)
        {
            var tuple = GlobalIndexUtil.ToIntTuple(term.DimensionIndex, term.Layer, term.LocalIndex);
            return TryGetInstance(tuple, term.InstanceIndex, out instance);
        }

        private static void SetTerminalCommand(WardenObjectiveEventData e)
        {
            var tuple = GlobalIndexUtil.ToIntTuple(e.DimensionIndex, e.Layer, e.LocalIndex);
            if (!Current.TryGetInstance(tuple, (uint)e.Count, out var terminal))
            {
                EOSLogger.Error($"SetTerminalCommand_Custom: Cannot find reactor for {e.Layer} or instance index ({tuple}, {e.Count})");
                return;
            }

            if (e.Enabled == true)
            {
                terminal.TrySyncSetCommandShow(e.TerminalCommand);
            }
            else
            {
                terminal.TrySyncSetCommandHidden(e.TerminalCommand);
            }

            EOSLogger.Debug($"SetTerminalCommand: Terminal_{terminal.m_serialNumber}, command '{e.TerminalCommand}' enabled ? {e.Enabled}");
        }

        private static void ToggleTerminalState(WardenObjectiveEventData e)
        {
            if (!SNet.IsMaster)
                return;

            var tuple = GlobalIndexUtil.ToIntTuple(e.DimensionIndex, e.Layer, e.LocalIndex);
            if (!Current.TryGetInstance(tuple, (uint)e.Count, out var terminal))
            {
                EOSLogger.Error($"ToggleTerminalState: terminal with index ({tuple}, {e.Count}) not found");
                return;
            }

            var wrapper = Current.GetTerminalWrapper(terminal);
            if (wrapper == null)
            {
                EOSLogger.Error($"ToggleTerminalState: internal error: terminal wrapper not found - {(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count)}");
                return;
            }

            wrapper.ChangeState(e.Enabled);
        }
    }
}
