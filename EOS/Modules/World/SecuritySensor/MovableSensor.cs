using ChainedPuzzles;
using GTFO.API.Extensions;
using UnityEngine;

namespace EOS.Modules.World.SecuritySensor
{
    public class MovableSensor
    {
        public GameObject MovableGO { get; private set; } = new();

        private readonly GameObject _graphicsGO = new();        
        private readonly CP_BasicMovable? _movingComp;

        public MovableSensor(SensorSettings setting)
        {
            MovableGO = UnityEngine.Object.Instantiate(SecuritySensorManager.MovableSensor);
            _movingComp = MovableGO.GetComponent<CP_BasicMovable>();
            _movingComp.Setup();

            Vector3 startPosition = setting.Position.ToVector3();
            Vector3 firstPosition = setting.MovingPosition.First().ToVector3();
            Vector3 lastPosition = setting.MovingPosition.Last().ToVector3();
            var scanPositions = setting.MovingPosition.ConvertAll(e => e.ToVector3()).AsEnumerable();

            if (!startPosition.Equals(firstPosition))
            {
                scanPositions = scanPositions.Prepend(startPosition);
            }
            if (!startPosition.Equals(lastPosition))
            {
                scanPositions = scanPositions.Append(startPosition);
            }

            _movingComp.ScanPositions = scanPositions.ToList().ToIl2Cpp();
            _movingComp.m_amountOfPositions = scanPositions.Count() - 1; // I'm not pretty sure why, but this is actually needed
            if (setting.MovingSpeedMulti > 0)
            {
                _movingComp.m_movementSpeed *= setting.MovingSpeedMulti;
            }

            _graphicsGO = MovableGO.transform.GetChild(0).gameObject;
        }

        public void StartMoving()
        {
            _graphicsGO.SetActive(true);
            _movingComp?.SyncUpdate();
            _movingComp?.StartMoving();
        }

        public void ResumeMoving()
        {
            _graphicsGO.SetActive(true);
            _movingComp?.ResumeMovement();
        }

        public void StopMoving()
        {
            _graphicsGO.SetActive(false);
            _movingComp?.StopMoving();
        }

        public void PauseMoving()
        {
            _graphicsGO.SetActive(false);
            _movingComp?.PauseMovement();
        }
    }
}
