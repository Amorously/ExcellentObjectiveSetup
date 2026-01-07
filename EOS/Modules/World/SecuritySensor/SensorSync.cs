using AmorLib.Networking;

namespace EOS.Modules.World.SecuritySensor
{
    internal sealed class SensorSync : SyncedEvent<IntPtr>
    {
        public override string Prefix => "EOS";
        public override string GUID => "SensorTrigger";

        protected override void Receive(IntPtr pointer)
        {
            SecuritySensorManager.Current.TriggerSensor(pointer);
        }

        protected override void ReceiveLocal(IntPtr packet)
        {
            Receive(packet);
        }
    }
}
