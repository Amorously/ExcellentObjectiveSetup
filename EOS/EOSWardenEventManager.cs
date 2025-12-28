using BepInEx.Unity.IL2CPP.Utils.Collections;
using GameData;
using GTFO.API.Extensions;
using GTFO.API.Utilities;
using Player;
using System.Collections;
using System.Collections.Immutable;
using UnityEngine;

namespace EOS
{
    public static class EOSWardenEventManager
    {
        public const uint AWOEventIDsStart = 10000;

        private static readonly Dictionary<uint, Action<WardenObjectiveEventData>> _eventDefinitions = new();
        private static readonly Dictionary<string, uint> _eventIDNameMap = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ImmutableHashSet<eWardenObjectiveEventType> _vanillaEventIDs = Enum.GetValues<eWardenObjectiveEventType>().ToImmutableHashSet();

        public static bool IsVanillaEventID(uint eventID) => _vanillaEventIDs.Contains((eWardenObjectiveEventType)eventID);
        public static bool IsAWOEventID(uint eventID) => eventID >= AWOEventIDsStart;
        public static bool HasEventDefinition(string eventName) => _eventIDNameMap.ContainsKey(eventName);
        public static bool HasEventDefinition(uint eventID) => _eventDefinitions.ContainsKey(eventID);

        public static bool AddEventDefinition(string eventName, uint eventID, Action<WardenObjectiveEventData> definition)
        {
            if(IsAWOEventID(eventID))
            {
                EOSLogger.Error($"EventID {eventID} is already used by AWO");
                return false;
            }

            if(IsVanillaEventID(eventID))
            {
                EOSLogger.Warning($"EventID {eventID}: overriding vanilla event!");
            }

            if (_eventIDNameMap.ContainsKey(eventName))
            {
                EOSLogger.Error($"AddEventDefinition: duplicate event name '{eventName}' or id '{eventID}'");
                return false;
            }

            _eventIDNameMap[eventName] = eventID;
            _eventDefinitions[eventID] = definition;

            EOSLogger.Debug($"EOSWardenEventManager: added event with name '{eventName}', id '{eventID}'");
            return true;
        }

        public static void ExecuteWardenEvent(WardenObjectiveEventData events, eWardenObjectiveEventTrigger trigger = eWardenObjectiveEventTrigger.None, bool ignoreTrigger = true)
            => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(events, trigger, ignoreTrigger);

        public static void ExecuteWardenEvents(List<WardenObjectiveEventData> events, eWardenObjectiveEventTrigger trigger = eWardenObjectiveEventTrigger.None, bool ignoreTrigger = true)
            => WardenObjectiveManager.CheckAndExecuteEventsOnTrigger(events.ToIl2Cpp(), trigger, ignoreTrigger);

        internal static void HandleEvent(WardenObjectiveEventData e, float currentDuration)
        {
            uint eventID = (uint)e.Type;
            if (!_eventDefinitions.ContainsKey(eventID))
            {
                EOSLogger.Error($"ExecuteEvent: event ID {eventID} doesn't have a definition");
                return;
            }

            var coroutine = CoroutineManager.StartCoroutine(Handle(e, currentDuration).WrapToIl2Cpp());
            WorldEventManager.m_worldEventEventCoroutines.Add(coroutine);
        }

        private static IEnumerator Handle(WardenObjectiveEventData e, float currentDuration)
        {
            uint eventID = (uint)e.Type;
            float delay = Mathf.Max(e.Delay - currentDuration, 0f);
            if (delay > 0f)
            {
                int reloadCount = CheckpointManager.CheckpointUsage;
                yield return new WaitForSeconds(delay);
                if (reloadCount < CheckpointManager.CheckpointUsage)
                {
                    EOSLogger.Warning($"Delayed event ID {eventID} aborted due to checkpoint reload");
                    yield break;
                }
            }

            if (WorldEventManager.GetCondition(e.Condition.ConditionIndex) != e.Condition.IsTrue)
            {
                yield break;
            }

            WardenObjectiveManager.DisplayWardenIntel(e.Layer, e.WardenIntel);
            if (e.DialogueID > 0u)
            {
                PlayerDialogManager.WantToStartDialog(e.DialogueID, -1, false, false);
            }

            if (e.SoundID > 0u)
            {
                WardenObjectiveManager.Current.m_sound.Post(e.SoundID, true);
                var line = e.SoundSubtitle.ToString();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    GuiManager.PlayerLayer.ShowMultiLineSubtitle(line);
                }
            }

            SafeInvoke.Invoke(_eventDefinitions[eventID], e);
        }
    }
}
