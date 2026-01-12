using AmorLib.Utils.JsonElementConverters;
using GameData;
using UnityEngine;

namespace EOS.Modules.World.SecuritySensor
{
    public enum SensorType
    {
        BASIC,
        MOVABLE,
    }

    public class SensorGroupSettings
    {
        public uint Index { get; set; } = uint.MaxValue;

        public List<SensorSettings> SensorGroup { set; get; } = new() { new() };

        public List<WardenObjectiveEventData> EventsOnTrigger { set; get; } = new() { };
    }

    public class SensorSettings
    {
        public Vec3 Position { get; set; } = new Vec3();

        public float Radius { get; set; } = 2.3f;

        public Color Color { get; set; } = new() { r = 0.9339623f, g = 0.1055641f, b = 0f, a = 0.2627451f };

        public LocaleText Text { get; set; } = new("S:_EC/uR_ITY S:/Ca_N");

        public Color TextColor { get; set; } = new() { r = 226f / 255f, g = 230f / 255f, b = 229 / 255f, a = 181f / 255f };

        public SensorType SensorType { get; set; } = SensorType.BASIC;

        public float MovingSpeedMulti { get; set; } = 1f;

        public List<Vec3> MovingPosition { get; set; } = new() { new() };
    }
}
