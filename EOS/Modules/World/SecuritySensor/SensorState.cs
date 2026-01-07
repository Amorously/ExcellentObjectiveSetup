namespace EOS.Modules.World.SecuritySensor
{
    public enum ActiveState
    {
        DISABLED,
        ENABLED,
    }

    public struct SensorGroupState
    {
        public ActiveState status { get; set; } = ActiveState.DISABLED;

        public SensorGroupState(SensorGroupState o) { status = o.status; }

        public SensorGroupState(ActiveState status) { this.status = status; }
    }

    public struct MovableSensorLerp
    {
        public float lerp { get; set; } = 0f;

        public MovableSensorLerp(MovableSensorLerp o) { lerp = o.lerp; }

        public MovableSensorLerp(float lerp) { this.lerp = lerp; }
    }
}
