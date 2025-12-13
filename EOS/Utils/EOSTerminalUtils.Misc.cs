using GameData;
using GTFO.API.Extensions;
using LevelGeneration;

namespace EOS.Utils
{
    public static partial class EOSTerminalUtils
    {
        public static List<LG_ComputerTerminal> FindTerminal(eDimensionIndex dimensionIndex, LG_LayerType layerType, eLocalZoneIndex localIndex, Predicate<LG_ComputerTerminal> predicate) 
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

        public static TerminalLogFileData GetLocalLog(this LG_ComputerTerminal terminal, string logName)
        {
            var localLogs = terminal.GetLocalLogs();
            logName = logName.ToUpperInvariant();
            return localLogs.ContainsKey(logName) ? localLogs[logName] : null!;
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
            var EMPTY = new List<WardenObjectiveEventData>();
            var terminalsInZone = terminal.SpawnNode.m_zone.TerminalsSpawnedInZone; // The order of spawned terminals is the same to the order of terminalplacementdatas in the datablock!
            int index = terminalsInZone.IndexOf(terminal);

            ExpeditionZoneData zoneData = terminal.SpawnNode?.m_zone.m_settings.m_zoneData ?? null!;
            if (zoneData == null)
            {
                EOSLogger.Error("GetCommandEvents: Cannot find target zone data.");
                return EMPTY;
            }
            if (index < 0 || index >= zoneData.TerminalPlacements.Count)
            {
                EOSLogger.Debug($"GetCommandEvents: TerminalDataIndex({index}), TargetZoneData.TerminalPlacements.Count == ({zoneData.TerminalPlacements.Count}) - maybe a custom terminal, skipped");
                return EMPTY;
            }

            var uniqueCommands = zoneData.TerminalPlacements[index].UniqueCommands;
            foreach(var cmd in uniqueCommands)
            {
                if (cmd.Command.ToLower().Equals(command.ToLower()))
                {
                    return cmd.CommandEvents.ToManaged();
                }
            }

            EOSLogger.Error($"GetCommandEvents: command '{command}' not found on {terminal.ItemKey}");
            return EMPTY;
        }
    }
}
