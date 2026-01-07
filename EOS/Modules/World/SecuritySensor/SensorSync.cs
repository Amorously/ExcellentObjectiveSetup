using AmorLib.Networking;

namespace EOS.Modules.World.SecuritySensor
{
    internal sealed class SensorSync : SyncedEvent<int>
    {
        public override string GUID => "EOS-SensorTrigger";

        protected override void Receive(int packet)
        {
            SecuritySensorManager.Current.TriggerSensor(packet);
        }

        protected override void ReceiveLocal(int packet)
        {
            Receive(packet);
        }
    }
}
