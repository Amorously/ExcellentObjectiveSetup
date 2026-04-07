using GameData;
using Gear;
using Player;
using UnityEngine;

namespace EOS.Modules.World.EMP.Handlers
{
    public sealed class EMPPlayerFlashlightHandler : EMPHandler
    {
        public static EMPPlayerFlashlightHandler Instance { get; private set; } = null!;
        private PlayerInventoryBase _inventory = null!;
        private float _baseIntensity;
        private bool _flashlightWasOn;

        public override void Setup(GameObject gameObject, EMPController controller)
        {
            if (Instance != null)
            {
                EOSLogger.Warning("EMPPlayerFlashLightHandler: re-setup detected, despawning old instance");
                Instance.OnDespawn();
            }

            base.Setup(gameObject, controller);
            _inventory = gameObject.GetComponent<PlayerAgent>().Inventory;

            if (_inventory == null)
                EOSLogger.Error("EMPPlayerFlashLightHandler: PlayerAgent inventory was null?");

            Instance = this;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            Instance = null!;
        }

        public void OnFlashlightWielded(GearPartFlashlight flashlight)
        {
            _baseIntensity = GameDataBlockBase<FlashlightSettingsDataBlock>.GetBlock(flashlight.m_settingsID).intensity;
        }       

        protected override void DeviceOn()
        {
            if (_flashlightWasOn != _inventory.FlashlightEnabled)
                _inventory.Owner.Sync.WantsToSetFlashlightEnabled(_flashlightWasOn);
            _inventory.m_flashlight.intensity = _baseIntensity;
        }
        
        protected override void DeviceOff()
        {
            _flashlightWasOn = _inventory.FlashlightEnabled;
            if (!_flashlightWasOn) return;
            _inventory.Owner.Sync.WantsToSetFlashlightEnabled(false);
        }

        protected override void FlickerDevice()
        {
            if (!_inventory.FlashlightEnabled) return;
            _inventory.m_flashlight.intensity = EMPManager.Rand.NextSingle() * _baseIntensity;
        }
    }
}
