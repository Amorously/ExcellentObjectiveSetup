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
            if (Parent.Status != ActiveState.ENABLED) return;

            byte currentPlayersInSensor = 0;
            bool localPlayerIsInSensor = false;
            foreach (var player in PlayerManager.PlayerAgentsInLevel)
            {
                if (player.Owner.IsBot || !player.Alive) continue;
                if (Position.IsWithinSqrDistance(player.Position, _sqrRadius))
                {
                    currentPlayersInSensor++;
                    localPlayerIsInSensor |= player.IsLocallyOwned;
                }
            }

            if (currentPlayersInSensor > _lastPlayersInSensor && localPlayerIsInSensor)
            {
                Parent.TriggerSensor();
            }
            _lastPlayersInSensor = currentPlayersInSensor;
        }
    }
}
