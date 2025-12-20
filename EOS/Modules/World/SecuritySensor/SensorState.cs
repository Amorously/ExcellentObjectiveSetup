namespace EOS.Modules.World.SecuritySensor
{
    public enum ActiveState
    {
        DISABLED,
        ENABLED,
    }

    public struct SensorGroupState
    {
        public ActiveState status;

        public SensorGroupState(SensorGroupState o) { status = o.status; }

        public SensorGroupState(ActiveState status) { this.status = status; }
    }

    public struct MovableSensorLerp
    {
        public float lerp;

        public MovableSensorLerp(MovableSensorLerp o) { lerp = o.lerp; }

        public MovableSensorLerp(float lerp) { this.lerp = lerp; }
    }
}
