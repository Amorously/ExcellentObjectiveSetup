using AmorLib.Utils;
using ChainedPuzzles;
using EOS.BaseClasses;
using EOS.Modules.Objectives.TerminalUplink;
using EOS.Modules.Tweaks.TerminalTweak;
using GameData;
using LevelGeneration;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Instances
{
    public sealed class TerminalInstanceManager: InstanceManager<LG_ComputerTerminal>
    {
        public enum TerminalWardenEvents 
        { 
            EOSSetTerminalCommand = 600,
            EOSToggleTerminalState = 601,
        }

        public static TerminalInstanceManager Current { get; private set; } = new();
        public static ImmutableList<TERM_Command> UNIQUE_CMDS { get; } = new List<TERM_Command>() 
        {
            TERM_Command.UniqueCommand1,
            TERM_Command.UniqueCommand2,
            TERM_Command.UniqueCommand3,
            TERM_Command.UniqueCommand4,
            TERM_Command.UniqueCommand5,
        }.ToImmutableList();

        private Dictionary<IntPtr, LG_ComputerTerminal> UniqueCommandChainPuzzles { get; } = new(); // key: ChainedPuzzleInstance.Pointer

        private Dictionary<IntPtr, TerminalWrapper> Wrappers { get; } = new();

        public TerminalInstanceManager()
        {
            EOSWardenEventManager.AddEventDefinition(TerminalWardenEvents.EOSSetTerminalCommand.ToString(), (uint)TerminalWardenEvents.EOSSetTerminalCommand, SetTerminalCommand);
            EOSWardenEventManager.AddEventDefinition(TerminalWardenEvents.EOSToggleTerminalState.ToString(), (uint)TerminalWardenEvents.EOSToggleTerminalState, ToggleTerminalState);
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnBuildDone() // GatherUniqueCommandChainPuzzles
        {
            foreach (var terminalsInZone in Index2Instance.Values)
            {
                foreach (var t in terminalsInZone)
                {
                    foreach (var cmd in UNIQUE_CMDS)
                    {
                        if (!t.m_command.m_commandsPerEnum.ContainsKey(cmd)) continue;
                        var cmdName = t.m_command.m_commandsPerEnum[cmd];
                        var events = t.GetUniqueCommandEvents(cmdName);

                        for (int i = 0; i < events.Count; i++)
                        {
                            if (t.TryGetChainPuzzleForCommand(cmd, i, out var cp))
                            {
                                UniqueCommandChainPuzzles[cp.Pointer] = t;
                            }
                        }
                    }
                }
            }
        }

        protected override void OnLevelCleanup()
        {
            UniqueCommandChainPuzzles.Clear();
            Wrappers.Clear();
            base.OnLevelCleanup();
        }

        public override (int dim, int layer, int zone) GetGlobalIndex(LG_ComputerTerminal instance)
        {
            if(instance.SpawnNode == null)
            {
                if(instance.ConnectedReactor != null)
                {
                    return instance.ConnectedReactor.SpawnNode.m_zone.ToIntTuple();
                }
                else
                {
                    throw new ArgumentException("LG_ComputerTerminal: both SpawnNode and ConnectedReactor are null!");
                }
            }

            return instance.SpawnNode.m_zone.ToIntTuple();
        }
        
        public bool TryGetInstance(Terminal term, [MaybeNullWhen(false)] out LG_ComputerTerminal instance)
        {
            var tuple = GlobalIndexUtil.ToIntTuple(term.DimensionIndex, term.Layer, term.LocalIndex);
            return TryGetInstance(tuple, term.InstanceIndex, out instance);
        }

        public override uint Register(LG_ComputerTerminal instance)
        {
            if (instance.SpawnNode == null)
            {
                EOSLogger.Error("Terminal has null spawn node!");
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
            if(Wrappers.ContainsKey(terminal.Pointer))
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

            TerminalWrapper t = TerminalWrapper.Instantiate(terminal, allotedID);
            Wrappers[terminal.Pointer] = t;
        }

        public TerminalWrapper GetTerminalWrapper(LG_ComputerTerminal terminal) => Wrappers.ContainsKey(terminal.Pointer) ? Wrappers[terminal.Pointer] : null!;

        public bool TryGetParentTerminal(ChainedPuzzleInstance cpInstance, out LG_ComputerTerminal terminal) => UniqueCommandChainPuzzles.TryGetValue(cpInstance.Pointer, out terminal!);

        public bool TryGetParentTerminal(IntPtr pointer, out LG_ComputerTerminal terminal) => UniqueCommandChainPuzzles.TryGetValue(pointer, out terminal!);

        public void SetTerminalCommand(WardenObjectiveEventData e)
        {
            var tuple = GlobalIndexUtil.ToIntTuple(e.DimensionIndex, e.Layer, e.LocalIndex);
            if (!TryGetInstance(tuple, (uint)e.Count, out var terminal))
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

        public void ToggleTerminalState(WardenObjectiveEventData e)
        {
            var tuple = GlobalIndexUtil.ToIntTuple(e.DimensionIndex, e.Layer, e.LocalIndex);
            if (!TryGetInstance(tuple, (uint)e.Count, out var terminal))
            {
                EOSLogger.Error($"ToggleTerminalState: terminal with index ({tuple}, {e.Count}) not found");
                return;
            }

            var wrapper = GetTerminalWrapper(terminal);
            if (wrapper == null)
            {
                EOSLogger.Error($"ToggleTerminalState: internal error: terminal wrapper not found - {(e.DimensionIndex, e.Layer, e.LocalIndex, e.Count)}");
                return;
            }

            wrapper.ChangeState(e.Enabled);
        }
    }
}
