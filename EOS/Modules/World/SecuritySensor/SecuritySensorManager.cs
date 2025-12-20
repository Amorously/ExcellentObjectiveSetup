using EOS.BaseClasses;
using GameData;
using GTFO.API;
using GTFO.API.Utilities;
using UnityEngine;

namespace EOS.Modules.World.SecuritySensor
{
    public sealed class SecuritySensorManager : GenericExpeditionDefinitionManager<SensorGroupSettings, SecuritySensorManager>
    {
        public enum SensorEventType
        {
            ToggleSensorGroupState = 400,
            ToggleAllSensorGroups = 401
        }

        protected override string DEFINITION_NAME => "SecuritySensor";

        public static GameObject CircleSensor { get; private set; }
        public static GameObject MovableSensor { get; private set; }

        private readonly List<SensorGroup> _sensorGroups = new();
        private readonly Dictionary<IntPtr, int> _sensorGroupIndex = new();

        static SecuritySensorManager()
        {
            EOSWardenEventManager.AddEventDefinition(SensorEventType.ToggleSensorGroupState.ToString(), (uint)SensorEventType.ToggleSensorGroupState, ToggleSensorGroup);
            EOSWardenEventManager.AddEventDefinition(SensorEventType.ToggleAllSensorGroups.ToString(), (uint)SensorEventType.ToggleAllSensorGroups, ToggleAllSensorGroups);
            
            try
            {
                CircleSensor = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/CircleSensor.prefab");
                MovableSensor = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/MovableSensor.prefab");
            }
            catch (Exception ex)
            {
                CircleSensor = new();
                MovableSensor = new();
                EOSLogger.Error($"Failed to load security sensor prefabs!:\n{ex}");
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            base.FileChanged(e);
            OnBuildStart();
            OnExpeditionStarted();
        }

        protected override void OnBuildStart()
        {
            OnLevelCleanup();
            if (!GenericExpDefinitions.TryGetValue(CurrentMainLevelLayout, out var def)) return;
            def.Definitions.ForEach(BuildSensorGroup);
        }

        protected override void OnExpeditionStarted()
        {
            _sensorGroups.ForEach(sg => sg.StartMovingMovables());
        }

        protected override void OnLevelCleanup()
        {
            _sensorGroups.ForEach(sg => sg.Destroy());
            _sensorGroups.Clear();
            _sensorGroupIndex.Clear();
        }

        private void BuildSensorGroup(SensorGroupSettings sensorGroupSettings)
        {
            int groupIndex = sensorGroupSettings.Index == uint.MaxValue ? _sensorGroups.Count : (int)sensorGroupSettings.Index;
            var sg = new SensorGroup(sensorGroupSettings, groupIndex);
            _sensorGroups.Add(sg);

            foreach (var go in sg.BasicSensors)
            {
                _sensorGroupIndex[go.Pointer] = groupIndex;
            }

            foreach (var m in sg.MovableSensors)
            {
                _sensorGroupIndex[m.MovableGO.Pointer] = groupIndex;
            }

            EOSLogger.Debug($"SensorGroup_{groupIndex} built");
        }

        internal void SensorTriggered(IntPtr pointer)
        {
            if (!_sensorGroupIndex.ContainsKey(pointer))
            {
                EOSLogger.Error($"Triggering a sensor but doesn't find its corresponding sensor group! This should not happen!");
                return;
            }

            int groupIndex = _sensorGroupIndex[pointer];
            if (groupIndex < 0 || groupIndex >= _sensorGroups.Count)
            {
                EOSLogger.Error($"TriggerSensor: invalid SensorGroup index {groupIndex}");
                return;
            }

            EOSLogger.Warning($"TriggerSensor: SensorGroup_{groupIndex} triggered");
            EOSWardenEventManager.ExecuteWardenEvents(_sensorGroups[groupIndex].Settings.EventsOnTrigger);
        }

        private static void ToggleSensorGroup(WardenObjectiveEventData e)
        {
            int groupIndex = Current._sensorGroups.FindIndex(sg => sg.Settings.Index != uint.MaxValue && sg.Settings.Index == e.Count);
            if (groupIndex == -1)
                groupIndex = e.Count;

            if (groupIndex < 0 || groupIndex >= Current._sensorGroups.Count)
            {
                EOSLogger.Error($"ToggleSensorGroup: invalid SensorGroup index {groupIndex}");
                return;
            }

            Current._sensorGroups[groupIndex].ChangeToState(e.Enabled ? ActiveState.ENABLED : ActiveState.DISABLED);
        }

        private static void ToggleAllSensorGroups(WardenObjectiveEventData e)
        {
            foreach (var sg in Current._sensorGroups)
            {
                sg.ChangeToState(e.Enabled ? ActiveState.ENABLED : ActiveState.DISABLED);
            }
        }
    }
}
