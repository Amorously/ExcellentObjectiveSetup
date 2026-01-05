using AmorLib.Utils;
using EOS.Modules.Objectives.Reactor;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;

namespace EOS.Utils
{
    public static partial class EOSTerminalUtil
    {
        public static List<LG_ComputerTerminal> FindTerminals((int dim, int layer, int zone) gIndex, Predicate<LG_ComputerTerminal> predicate)
        {
            return FindTerminals((eDimensionIndex)gIndex.dim, (LG_LayerType)gIndex.layer, (eLocalZoneIndex)gIndex.zone, predicate);
        }

        public static List<LG_ComputerTerminal> FindTerminals(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, Predicate<LG_ComputerTerminal> predicate) 
        {
            if (!Builder.CurrentFloor.TryGetZoneByLocalIndex(dimensionIndex, layerType, localIndex, out var zone) || zone == null)
            {
                EOSLogger.Error($"SelectTerminal: Could NOT find zone {(dimensionIndex, layerType, localIndex)}");
                return null!;
            }

            if (zone.TerminalsSpawnedInZone.Count == 0)
            {
                EOSLogger.Error($"SelectTerminal: Could not find any terminals in zone {(dimensionIndex, layerType, localIndex)}");
                return null!;
            }

            List<LG_ComputerTerminal> result = new();
            foreach (var terminal in zone.TerminalsSpawnedInZone)
            {
                if(predicate != null)
                {
                    if(predicate(terminal))  
                        result.Add(terminal); 
                }
                else
                {
                    result.Add(terminal);
                }
            }

            return result;
        }

        public static TerminalLogFileData? GetLocalLog(this LG_ComputerTerminal terminal, string logName)
        {
            var localLogs = terminal.GetLocalLogs();
            logName = logName.ToUpperInvariant();
            return localLogs.ContainsKey(logName) ? localLogs[logName] : null;
        }

        public static void ResetInitialOutput(this LG_ComputerTerminal terminal)
        {
            terminal.m_command.ClearOutputQueueAndScreenBuffer();
            terminal.m_command.AddInitialTerminalOutput();

            if (terminal.IsPasswordProtected)
            {
                terminal.m_command.AddPasswordProtectedOutput();
            }
        }

        public static List<WardenObjectiveEventData> GetUniqueCommandEvents(this LG_ComputerTerminal terminal, string command)
        {
            var node = terminal.SpawnNode;
            node ??= CourseNodeUtil.GetCourseNode(terminal.m_position, Dimension.GetDimensionFromPos(terminal.m_position).DimensionIndex);
            if (node == null)
            {
                EOSLogger.Error("GetCommandEvents: Cannot find a terminal spawn node");
                return new();
            }

            var zoneData = node.m_zone.m_settings?.m_zoneData;
            if (zoneData == null)
            {
                EOSLogger.Error("GetCommandEvents: Cannot find terminal zone data");
                return new();
            }

            var terminalsInZone = node.m_zone.TerminalsSpawnedInZone;
            int index = terminalsInZone.IndexOf(terminal);
            if (index < 0)
            {
                EOSLogger.Warning("GetCommandEvents: terminal not found in TerminalsSpawnedInZone");
                return new();
            }

            var placementData = zoneData.TerminalPlacements ?? new();
            var specificData = zoneData.SpecificTerminalSpawnDatas ?? new();
            List<CustomTerminalCommand> uniqueCommands = new();
            if (terminal.ConnectedReactor != null)
            {
                if (ReactorShutdownObjectiveManager.Current.TryGetDefinition(terminal.ConnectedReactor, out var def))
                {
                    uniqueCommands = def.ReactorTerminal.UniqueCommands.ConvertAll(cmd => cmd.ToVanillaDataType());
                }
                else if (ReactorStartupOverrideManager.Current.TryGetDefinition(terminal.ConnectedReactor, out var def2))
                {
                    uniqueCommands = def2.ReactorTerminal.UniqueCommands.ConvertAll(cmd => cmd.ToVanillaDataType());
                }
                else
                {
                    return new();
                }
            }
            else if (index >= placementData.Count && (index - placementData.Count) < specificData.Count)
            {
                uniqueCommands = specificData[index - placementData.Count].UniqueCommands.ToManaged();
            }
            else if (index < placementData.Count)
            {
                uniqueCommands = placementData[index].UniqueCommands.ToManaged();
            }
            else
            {
                EOSLogger.Warning($"GetCommandEvents: skipped! Terminal_{terminal.PublicName}, TerminalDataIndex({index})");
                return new();
            }

            foreach (var cmd in uniqueCommands)
            {
                if (cmd.Command.Equals(command, StringComparison.InvariantCultureIgnoreCase))
                {
                    return cmd.CommandEvents.ToManaged();
                }
            }

            EOSLogger.Warning($"GetCommandEvents: command '{command}' not found on {terminal.ItemKey}");
            return new();
        }
    }
}
