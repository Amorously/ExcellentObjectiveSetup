using AmorLib.Utils.Extensions;
using UnityEngine;

namespace EOS.Modules.World.EMP
{
    public sealed class EMPShock : IEMPSource
    {
        public Vector3 Position { get; }
        public float Range { get; }
        public float SqrRange { get; }
        public float EndTime { get; }
        public ItemToDisable ItemToDisable { get; } = new(true, true, true, true, true, true, true);

        public bool IsActive => Clock.Time < EndTime;
        public bool InRange(Vector3 point) => point.IsWithinSqrDistance(Position, SqrRange);

        public EMPShock(Vector3 position, float range, float endTime)
        {
            Position = position;
            Range = range;
            SqrRange = range * range;
            EndTime = endTime;
        }
    }
}
