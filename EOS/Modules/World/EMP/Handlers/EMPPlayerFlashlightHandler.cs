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

        public override void Setup(GameObject gameObject)
        { 
            Instance?.OnDespawn();
            base.Setup(gameObject);
            _inventory = gameObject.GetComponent<PlayerAgent>().Inventory;

            if (_inventory?.m_flashlight != null)
                _baseIntensity = _inventory.m_flashlight.intensity;

            Instance = this;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            if (Instance == this) Instance = null!;
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
