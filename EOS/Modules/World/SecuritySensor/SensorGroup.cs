using AmorLib.Networking.StateReplicators;
using TMPro;
using UnityEngine;

namespace EOS.Modules.World.SecuritySensor
{
    public class SensorGroup
    {
        public int SensorGroupIndex { get; private set; }
        public SensorGroupSettings Settings { get; private set; }
        public StateReplicator<SensorGroupState>? Replicator { get; private set; }
        public ActiveState Status { get; private set; } = ActiveState.ENABLED;
        
        public IEnumerable<GameObject> BasicSensors => _basicSensors;
        private readonly List<GameObject> _basicSensors = new();

        public IEnumerable<MovableSensor> MovableSensors => _movableSensors;
        private readonly List<MovableSensor> _movableSensors = new();

        public SensorGroup(SensorGroupSettings sensorGroupSettings, int sensorGroupIndex)
        {
            SensorGroupIndex = sensorGroupIndex;
            Settings = sensorGroupSettings;

            foreach (var setting in sensorGroupSettings.SensorGroup)
            {
                Vector3 position = setting.Position;
                if (position == Vector3.zeroVector) continue;

                GameObject sensorGO = new();
                switch (setting.SensorType)
                {
                    case SensorType.BASIC:
                        sensorGO = UnityEngine.Object.Instantiate(SecuritySensorManager.CircleSensor);
                        _basicSensors.Add(sensorGO);
                        break;

                    case SensorType.MOVABLE:
                        var movableSensor = new MovableSensor(setting);
                        if (setting.MovingPosition.Count < 1)
                        {
                            EOSLogger.Error("SensorGroup: at least 1 moving position is required to setup T-Sensor!");
                            continue;
                        }
                        sensorGO = movableSensor.MovableGO;
                        _movableSensors.Add(movableSensor);
                        break;

                    default:
                        EOSLogger.Error($"Unsupported SensorType {setting.SensorType}, skipped");
                        continue;
                }

                sensorGO.transform.SetPositionAndRotation(position, Quaternion.identityQuaternion);
                sensorGO.transform.localPosition += Vector3.up * 0.6f / 3.7f;
                sensorGO.transform.localScale = new(setting.Radius, setting.Radius, setting.Radius);

                sensorGO.AddComponent<SensorColliderComp>().Setup(this, setting.Radius);

                sensorGO.transform.GetChild(0).GetChild(1)
                    .gameObject.GetComponentInChildren<Renderer>()
                    .material.SetColor("_ColorA", setting.Color);

                var infoGO = sensorGO.transform.GetChild(0).GetChild(2); //.gameObject.GetComponentInChildren<TextMeshPro>();
                var corruptedTMPGO = infoGO.GetChild(0).gameObject;
                corruptedTMPGO.transform.SetParent(null);
                GameObject.Destroy(corruptedTMPGO);

                var text = VanillaTMPUtil.Instantiate(infoGO.gameObject)?.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    text.SetText(setting.Text);
                    text.m_fontColor = text.m_fontColor32 = setting.TextColor;
                }
                else
                {
                    EOSLogger.Error("SensorGroup: NO TEXT!!");
                }
                sensorGO.SetActive(true);
            }

            uint allottedID = EOSNetworking.AllotReplicatorID();
            if (allottedID == EOSNetworking.INVALID_ID)
            {
                EOSLogger.Error("SensorGroup: replicator IDs depleted, cannot setup StateReplicator");
                return;
            }
            
            Replicator = StateReplicator<SensorGroupState>.Create(allottedID, new() { status = ActiveState.ENABLED }, LifeTimeType.Session);
            Replicator!.OnStateChanged += OnStateChanged;
        }

        public void Destroy()
        {
            _basicSensors.ForEach(UnityEngine.Object.Destroy);
            _movableSensors.ForEach(m => UnityEngine.Object.Destroy(m.MovableGO));
            Replicator?.Unload();
        }

        public void ChangeState(ActiveState status) 
        {
            EOSLogger.Debug($"ChangeState: SecuritySensorGroup_{SensorGroupIndex} changed to state {status}");
            Replicator?.SetState(new() { status = status });
        }        

        private void OnStateChanged(SensorGroupState _, SensorGroupState state, bool isRecall)
        {
            if (Status != state.status)
            {
                Status = state.status;
                _basicSensors.ForEach(sensorGO => sensorGO.SetActive(Status == ActiveState.ENABLED));

                if (Status == ActiveState.ENABLED)
                    ResumeMovingMovables();
                else
                    PauseMovingMovables();
            }
        }

        public void StartMovingMovables() => _movableSensors.ForEach(movable => movable.StartMoving());

        public void PauseMovingMovables() => _movableSensors.ForEach(movable => movable.PauseMoving());

        public void ResumeMovingMovables() => _movableSensors.ForEach(movable => movable.ResumeMoving());
    }
}
