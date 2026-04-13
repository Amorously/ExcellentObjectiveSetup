using AK;
using GTFO.API;
using UnityEngine;

namespace EOS.Modules.World.EMP.Handlers
{
    public sealed class EMPSentryHandler : EMPHandler
    {        
        public static IEnumerable<EMPSentryHandler> AllSentries => s_instances;
        private static readonly List<EMPSentryHandler> s_instances = new();
        private SentryGunInstance _sentry = null!;
        private SentryGunInstance_ScannerVisuals_Plane _visuals = null!;

        static EMPSentryHandler()
        {
            LevelAPI.OnBuildStart += s_instances.Clear;
            LevelAPI.OnLevelCleanup += s_instances.Clear;
        }

        public override void Setup(GameObject gameObject)
        {
            base.Setup(gameObject);
            _sentry = gameObject.GetComponent<SentryGunInstance>();
            _visuals = gameObject.GetComponent<SentryGunInstance_ScannerVisuals_Plane>();

            if (_sentry == null || _visuals == null)
            {
                EOSLogger.Error($"EMPSentryHandler: missing components, will not setup! [Sentry: {_sentry != null}, Visuals: {_visuals != null}]");
                OnDespawn();
                return;
            }

            s_instances.Add(this);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            s_instances.Remove(this);
        }

        protected override void DeviceOn()
        {
            _sentry.m_isSetup = true;
            _sentry.m_visuals.SetVisualStatus(eSentryGunStatus.BootUp);
            _sentry.m_isScanning = false;
            _sentry.m_startScanTimer = Clock.Time + _sentry.m_initialScanDelay;
            _sentry.Sound.Post(EVENTS.SENTRYGUN_LOW_AMMO_WARNING);
        }
        
        protected override void DeviceOff()
        {
            _visuals.m_scannerPlane.SetColor(Color.clear);
            _visuals.UpdateLightProps(Color.clear, false);
            _sentry.m_isSetup = false;
            _sentry.m_isScanning = false;
            _sentry.m_isFiring = false;
            _sentry.Sound.Post(EVENTS.SENTRYGUN_STOP_ALL_LOOPS);
        }

        protected override void FlickerDevice()
        {
            _sentry.StopFiring();
            switch (EMPManager.Rand.Next(0, 3))
            {
                case 0: 
                    _visuals.SetVisualStatus(eSentryGunStatus.OutOfAmmo, true); 
                    break;

                case 1: 
                    _visuals.SetVisualStatus(eSentryGunStatus.Scanning, true); 
                    break;

                case 2: 
                    _visuals.SetVisualStatus(eSentryGunStatus.HasTarget, true); 
                    break;
            }
        }
    }
}