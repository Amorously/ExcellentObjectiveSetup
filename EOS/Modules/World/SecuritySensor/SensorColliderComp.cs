using AmorLib.Utils.Extensions;
using Il2CppInterop.Runtime.Attributes;
using Player;
using UnityEngine;

namespace EOS.Modules.World.SecuritySensor
{
    public class SensorColliderComp : MonoBehaviour
    {
        public const float CHECK_INTERVAL = 0.1f;

        [HideFromIl2Cpp]
        public SensorGroup Parent { get; internal set; } = null!;
       
        private Vector3 Position => gameObject.transform.position;
        private float _sqrRadius;
        private float _nextCheckTime = float.NaN;
        private byte _lastPlayersInSensor = 0;

        [HideFromIl2Cpp]
        public void Setup(SensorGroup parent, float radius)
        {
            Parent = parent;
            _sqrRadius = radius * radius;
        }

        public void Update()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;
            if (!float.IsNaN(_nextCheckTime) && Clock.Time < _nextCheckTime) return;
            _nextCheckTime = Clock.Time + CHECK_INTERVAL;
            if (Parent.State != ActiveState.ENABLED) return;

            byte current_playersInSensor = 0;
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if (player.Owner.IsBot || !player.Alive) 
                    continue;
                if (Position.IsWithinSqrDistance(player.Sync.m_locomotionData.Pos, _sqrRadius))
                    current_playersInSensor++;
            }

            if (current_playersInSensor > _lastPlayersInSensor)
            {
                SecuritySensorManager.Current.SensorTriggered(gameObject.Pointer);
            }
            _lastPlayersInSensor = current_playersInSensor;
        }
    }
}
