using AmorLib.Utils;
using ChainedPuzzles;
using EOS.BaseClasses.CustomTerminalDefinition;
using GameData;
using GTFO.API.Extensions;
using LevelGeneration;
using UnityEngine;

namespace EOS.Utils
{
    public static partial class EOSTerminalUtils
    {
        public static void AddUniqueCommand(LG_ComputerTerminal terminal, CustomCommand cmd)
        {
            if (terminal.m_command.m_commandsPerString.ContainsKey(cmd.Command))
            {
                EOSLogger.Error($"Duplicate command name: '{cmd.Command}', cannot add command");
                return;
            }
            if (!terminal.m_command.TryGetUniqueCommandSlot(out var uniqueCmdSlot))
            {
                EOSLogger.Error($"Cannot get more unique command slot, max: 5");
                return;
            }

            terminal.m_command.AddCommand(uniqueCmdSlot, cmd.Command, cmd.CommandDesc, cmd.SpecialCommandRule, cmd.CommandEvents.ToIl2Cpp(), cmd.PostCommandOutputs.ToIl2Cpp());
            for (int i = 0; i < cmd.CommandEvents.Count; i++)
            {
                var e = cmd.CommandEvents[i];
                if (e.ChainPuzzle != 0u)
                {
                    if (!DataBlockHelper.TryGetBlock<ChainedPuzzleDataBlock>(e.ChainPuzzle, out var block))
                        continue;

                    LG_Area sourceArea;
                    Transform transform;
                    if (terminal.ConnectedReactor == null)
                    {
                        sourceArea = terminal.SpawnNode.m_area;
                        transform = terminal.m_wardenObjectiveSecurityScanAlign;
                    }
                    else
                    {
                        sourceArea = terminal.ConnectedReactor?.SpawnNode?.m_area ?? null!;
                        transform = terminal.ConnectedReactor?.m_chainedPuzzleAlign ?? null!;
                    }

                    if (sourceArea == null)
                    {
                        EOSLogger.Error($"Terminal Source Area is not found! Cannot create chained puzzle for command {cmd.Command}!");
                        continue;
                    }

                    ChainedPuzzleInstance puzzleInstance = ChainedPuzzleManager.CreatePuzzleInstance(block, sourceArea, transform.position, transform, e.UseStaticBioscanPoints);
                    var events = cmd.CommandEvents.GetRange(i, cmd.CommandEvents.Count - i); 
                    puzzleInstance.OnPuzzleSolved += new Action(() => 
                    {
                        EOSWardenEventManager.ExecuteWardenEvents(events);
                    });

                    terminal.SetChainPuzzleForCommand(uniqueCmdSlot, i, puzzleInstance);                    
                }
            }
        }
    }
}
