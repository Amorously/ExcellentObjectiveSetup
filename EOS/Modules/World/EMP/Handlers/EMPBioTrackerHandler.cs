using AK;
using Gear;
using UnityEngine;

namespace EOS.Modules.World.EMP.Handlers
{
    public sealed class EMPBioTrackerHandler : EMPHandler
    {
        public static EMPBioTrackerHandler Instance { get; private set; } = null!;
        private EnemyScanner _scanner = null!;

        public override void Setup(GameObject gameObject, EMPController controller)
        {
            if (Instance != null)
            {
                EOSLogger.Warning("EMPBioTrackerHandler: re-setup detected, despawning old instance");
                Instance.OnDespawn();
            }

            base.Setup(gameObject, controller);
            _scanner = gameObject.GetComponent<EnemyScanner>();
            
            if (_scanner == null)
                EOSLogger.Error("EMPBioTrackerHandler: no EnemyScanner found on GameObject!");

            Instance = this;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            Instance = null!;
        }

        protected override void DeviceOn()
        {
            _scanner.m_graphics.m_display.enabled = true;
        }

        protected override void DeviceOff()
        {
            _scanner.Sound.Post(EVENTS.BIOTRACKER_TOOL_LOOP_STOP);
            _scanner.m_graphics.m_display.enabled = false;
        }

        protected override void FlickerDevice()
        {
            _scanner.enabled = EMPManager.RandCoin();
        }
    }
}
