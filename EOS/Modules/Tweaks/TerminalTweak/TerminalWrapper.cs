using AmorLib.Networking.StateReplicators;
using BepInEx.Unity.IL2CPP.Utils;
using EOS.Modules.Expedition;
using GameData;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace EOS.Modules.Tweaks.TerminalTweak
{
    public class TerminalWrapper
    {
        public LG_ComputerTerminal Terminal { get; private set; } = null!;
        public StateReplicator<TerminalState>? Replicator { get; private set; }
        
        private readonly Dictionary<string, List<WardenObjectiveEventData>> _logEventMap = new();
        private readonly List<string> _filenameList = new();
        private readonly ExpeditionTerminalsDefinition? _expTermDef = null;

        public TerminalWrapper(LG_ComputerTerminal term, uint replicatorID)
        {
            if (term == null || replicatorID == EOSNetworking.INVALID_ID) 
                return;

            Terminal = term;
            Replicator = StateReplicator<TerminalState>.Create(replicatorID, new(), LifeTimeType.Session);
            Replicator!.OnStateChanged += OnStateChanged;

            if (!ExpeditionDefinitionManager.Current.TryGetTerminalDefinitionFromInstance(term, out var defs))
                return;
            
            _expTermDef = new ExpeditionTerminalsDefinition // merge defs if there are more than one for same terminal
            {
                DimensionIndex = defs.First().DimensionIndex,
                Layer = defs.First().Layer,
                LocalIndex = defs.First().LocalIndex,
                InstanceIndex = defs.First().InstanceIndex,
                EventsOnApproach = defs.SelectMany(d => d.EventsOnApproach).ToList(),
                LogFiles = defs
                    .SelectMany(d => d.LogFiles)
                    .GroupBy(lf => lf.FileName)
                    .Select(g => new TerminalLogFileEvents
                    {
                        FileName = g.Key.ToUpperInvariant(),
                        EventsOnFileRead = g.SelectMany(lf => lf.EventsOnFileRead).ToList()
                    }).ToList(),
            };
            _expTermDef.LogFiles.ForEach(d =>
            {
                _logEventMap[d.FileName] = d.EventsOnFileRead;
                _filenameList.Add(d.FileName);
            });
        }

        public void ChangeState()
        {
            if (_expTermDef != null) 
                Replicator?.SetState(Replicator.State with { approached = true });
        }

        public void ChangeState(bool enabled)
        {
            Replicator?.SetState(Replicator.State with { enabled = enabled });
        }

        public void ChangeState(string filename)
        {
            int index = _filenameList.IndexOf(filename);
            if (_expTermDef == null || Replicator == null || index == -1) 
                return;

            bool[] currentBits = Replicator.State.Value;
            bool[] bits = new bool[_filenameList.Count];
            for (int i = 0; i < currentBits.Length && i < bits.Length; i++)
            {
                bits[i] = currentBits[i];
            }

            if (bits[index]) return; // log is already read
            bits[index] = true;
            Replicator?.SetState(Replicator.State with { Value = bits });
        }

        private void OnStateChanged(TerminalState oldState, TerminalState state, bool isRecall)
        {
            if (oldState.enabled != state.enabled)
            {
                bool active = state.enabled;
                Terminal.OnProximityExit();
                var interact = Terminal.GetComponentInChildren<Interact_ComputerTerminal>(true);
                if (interact != null)
                {
                    interact.enabled = active;
                    interact.SetActive(active);
                }

                Terminal.m_interfaceScreen.SetActive(active);
                Terminal.m_loginScreen.SetActive(active);

                if (Terminal.m_text != null)
                    Terminal.m_text.enabled = active;

                if (!active)
                {
                    var interactionSource = Terminal.m_localInteractionSource;
                    if (interactionSource?.FPItemHolder?.InTerminalTrigger == true)
                    {
                        Terminal.ExitFPSView();
                    }
                }
            }

            if (_expTermDef == null || isRecall)
                return;

            if (!oldState.approached && state.approached)
            {
                EOSWardenEventManager.ExecuteWardenEvents(_expTermDef.EventsOnApproach);
            }

            bool[] oldBits = oldState.Value;
            bool[] bits = state.Value;
            for (int i = 0; i < _filenameList.Count; i++)
            {
                bool oldStateLogRead = i < oldBits.Length && oldBits[i];
                bool stateLogRead = i < bits.Length && bits[i];
                if (!oldStateLogRead && stateLogRead)
                {
                    if (_logEventMap.TryGetValue(_filenameList[i], out var eData))
                    {
                        Terminal.StartCoroutine(DoEvents(eData));
                    }
                }
            }
        }

        private static IEnumerator DoEvents(List<WardenObjectiveEventData> eData)
        {
            yield return new WaitForSeconds(3f); // wait for line output done, i.e. log viewable
            EOSWardenEventManager.ExecuteWardenEvents(eData);
        }
    }
}
