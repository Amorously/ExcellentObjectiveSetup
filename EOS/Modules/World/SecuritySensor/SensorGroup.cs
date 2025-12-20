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
        public ActiveState State { get; private set; } = ActiveState.ENABLED;
        
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
                var position = setting.Position.ToVector3();
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
                            EOSLogger.Error($"SensorGroup: At least 1 moving position required to setup T-Sensor!");
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

                sensorGO.AddComponent<SensorCollider>().Setup(this, setting.Radius);

                sensorGO.transform.GetChild(0).GetChild(1)
                    .gameObject.GetComponentInChildren<Renderer>()
                    .material.SetColor("_ColorA", setting.Color);

                var infoGO = sensorGO.transform.GetChild(0).GetChild(2); //.gameObject.GetComponentInChildren<TextMeshPro>();
                var corruptedTMPProGO = infoGO.GetChild(0).gameObject;
                corruptedTMPProGO.transform.SetParent(null);
                GameObject.Destroy(corruptedTMPProGO);

                var TMPProGO = VanillaTMPPros.Instantiate(infoGO.gameObject);
                var text = TMPProGO.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    text.SetText(setting.Text);
                    text.m_fontColor = text.m_fontColor32 = setting.TextColor;
                }
                else
                {
                    EOSLogger.Error("NO TEXT!");
                }
                sensorGO.SetActive(true);
            }

            uint allotedID = EOSNetworking.AllotReplicatorID();
            if (allotedID == EOSNetworking.INVALID_ID)
            {
                EOSLogger.Error($"SensorGroup.Instantiate: replicator ID depleted, cannot create StateReplicator...");
            }
            
            Replicator = StateReplicator<SensorGroupState>.Create(allotedID, new() { status = ActiveState.ENABLED }, LifeTimeType.Session);
            Replicator!.OnStateChanged += OnStateChanged;
        }

        public void Destroy()
        {
            _basicSensors.ForEach(UnityEngine.Object.Destroy);
            _movableSensors.ForEach(m => UnityEngine.Object.Destroy(m.MovableGO));
            Replicator?.Unload();
        }

        public void ChangeToState(ActiveState status) 
        {
            EOSLogger.Debug($"ChangeState: SecuritySensorGroup_{SensorGroupIndex} changed to state {status}");
            Replicator?.SetStateUnsynced(new() { status = status });
        }

        private void OnStateChanged(SensorGroupState _, SensorGroupState state, bool isRecall)
        {
            if (State != state.status)
            {
                State = state.status;
                _basicSensors.ForEach(sensorGO => sensorGO.SetActive(State == ActiveState.ENABLED));

                if (State == ActiveState.ENABLED)
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
