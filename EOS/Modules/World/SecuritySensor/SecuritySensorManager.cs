using EOS.BaseClasses;
using GameData;
using GTFO.API;
using GTFO.API.Utilities;
using SNetwork;
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

        internal static readonly SensorSync SyncTrigger = new();

        private readonly List<SensorGroup> _sensorGroups = new();
        private static readonly bool _flag;

        static SecuritySensorManager()
        {
            EOSWardenEventManager.AddEventDefinition(SensorEventType.ToggleSensorGroupState.ToString(), (uint)SensorEventType.ToggleSensorGroupState, ToggleSensorGroup);
            EOSWardenEventManager.AddEventDefinition(SensorEventType.ToggleAllSensorGroups.ToString(), (uint)SensorEventType.ToggleAllSensorGroups, ToggleAllSensorGroups);
            SyncTrigger.Setup();

            try
            {
                CircleSensor = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/CircleSensor.prefab");
                MovableSensor = AssetAPI.GetLoadedAsset<GameObject>("Assets/SecuritySensor/MovableSensor.prefab");

                if (CircleSensor == null || MovableSensor == null)
                    throw new Exception("Failed to load security sensor prefabs!");
            }
            catch (Exception ex)
            {
                _flag = true;
                CircleSensor = new();
                MovableSensor = new();
                EOSLogger.Error($"{ex}");
            }
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            base.FileChanged(e);
            OnBuildStart();
            OnEnterLevel();
        }

        protected override void OnBuildStart()
        {
            OnLevelCleanup();
            if (!GenericExpDefinitions.TryGetValue(CurrentMainLevelLayout, out var def)) return;
            def.Definitions.ForEach(BuildSensorGroup);
        }

        protected override void OnEnterLevel()
        {
            _sensorGroups.ForEach(sg => sg.StartMovingMovables());
        }

        protected override void OnLevelCleanup()
        {
            _sensorGroups.ForEach(sg => sg.Destroy());
            _sensorGroups.Clear();
        }

        private void BuildSensorGroup(SensorGroupSettings sensorGroupSettings)
        {
            if (_flag)
                FlagMsg();

            int groupIndex = sensorGroupSettings.Index == uint.MaxValue ? _sensorGroups.Count : (int)sensorGroupSettings.Index;
            var sg = new SensorGroup(sensorGroupSettings, groupIndex);
            _sensorGroups.Add(sg);

            EOSLogger.Debug($"SensorGroup_{groupIndex} built");
        }

        internal void TriggerSensor(int packet)
        {
            int groupIndex = _sensorGroups.FindIndex(sg => sg.SensorGroupIndex == packet);
            groupIndex = groupIndex == -1 ? packet : groupIndex;
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
            if (_flag)
                FlagMsg();
            
            if (!SNet.IsMaster)
                return;
            
            int groupIndex = Current._sensorGroups.FindIndex(sg => sg.SensorGroupIndex == e.Count);
            groupIndex = groupIndex == -1 ? e.Count : groupIndex;
            if (groupIndex < 0 || groupIndex >= Current._sensorGroups.Count)
            {
                EOSLogger.Error($"ToggleSensorGroup: invalid SensorGroup index {groupIndex}");
                return;
            }

            Current._sensorGroups[groupIndex].ChangeState(e.Enabled ? ActiveState.ENABLED : ActiveState.DISABLED);
        }

        private static void ToggleAllSensorGroups(WardenObjectiveEventData e)
        {
            if (_flag)
                FlagMsg();

            if (!SNet.IsMaster)
                return;

            foreach (var sg in Current._sensorGroups)
            {
                sg.ChangeState(e.Enabled ? ActiveState.ENABLED : ActiveState.DISABLED);
            }
        }

        private static void FlagMsg() => EOSLogger.Error("Failed to load security sensor prefabs during setup!");
    }
}
