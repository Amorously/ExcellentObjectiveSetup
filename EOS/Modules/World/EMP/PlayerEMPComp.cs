using AmorLib.Utils.Extensions;
using EOS.Modules.World.EMP.Handlers;
using Gear;
using Player;
using UnityEngine;

namespace EOS.Modules.World.EMP
{
    public class PlayerEMPComp : MonoBehaviour
    {
        private PlayerAgent _player = null!;
        private const float UPDATE_INTERVAL = 0.15f;
        private float _nextUpdateTime;        
        private readonly HashSet<int> _gearHelded = new();

        public void Awake()
        {
            _player = GetComponent<PlayerAgent>();
            SetupHUDHandler();
            SetupFlashlightHandler();
            EMPManager.InventoryWielded += OnInventoryWielded;
            EMPManager.FlashlightWielded += OnFlashlightWielded;
        }

        public void OnDestroy()
        {
            EMPManager.InventoryWielded -= OnInventoryWielded;
            EMPManager.FlashlightWielded -= OnFlashlightWielded;
            _gearHelded.Clear();
        }

        private void SetupHUDHandler()
        {
            _player.gameObject.AddComponent<EMPController>().AssignHandler(new EMPPlayerHudHandler());
            EOSLogger.Debug("EMP: PlayerHUD handler ready");
        }

        private void SetupFlashlightHandler()
        {
            _player.gameObject.AddComponent<EMPController>().AssignHandler(new EMPPlayerFlashlightHandler());
            EOSLogger.Debug("EMP: Flashlight handler ready");
        }

        private void OnInventoryWielded(InventorySlot slot)
        {
            switch (slot)
            {
                case InventorySlot.GearStandard:
                case InventorySlot.GearSpecial:
                    SetupWeaponHandler(slot);
                    break;

                case InventorySlot.GearClass:
                    SetupToolHandler();
                    break;
            }
        }

        private void SetupWeaponHandler(InventorySlot slot)
        {
            if (!PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(slot, out var item) || _gearHelded.Contains(item.Instance.GetInstanceID()))
                return;

            _gearHelded.Add(item.Instance.GetInstanceID());
            item.Instance.gameObject.AddOrGetComponent<EMPController>().AssignHandler(new EMPGunSightHandler());
            EOSLogger.Debug($"EMP: GunSight handler ready for slot {slot}");
        }

        private void SetupToolHandler()
        {
            if (!PlayerBackpackManager.LocalBackpack.TryGetBackpackItem(InventorySlot.GearClass, out var item) || _gearHelded.Contains(item.Instance.GetInstanceID()))
                return;

            _gearHelded.Add(item.Instance.GetInstanceID());

            if (item.Instance.gameObject.GetComponent<EnemyScanner>() == null)
                return;

            item.Instance.gameObject.AddOrGetComponent<EMPController>().AssignHandler(new EMPBioTrackerHandler());
            EOSLogger.Debug("EMP: BioTracker handler ready");
        }

        private void OnFlashlightWielded(GearPartFlashlight flashlight)
        {
            EMPPlayerFlashlightHandler.Instance?.OnFlashlightWielded(flashlight);
        }

        public void Update()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;

            float now = Clock.Time;
            if (now < _nextUpdateTime) return;
            _nextUpdateTime = now + UPDATE_INTERVAL;

            EMPManager.Current.RemoveInactiveShocks();
            PersistentEMPProximityUpdate();
        }

        private void PersistentEMPProximityUpdate()
        {
            Vector3 playerPos = _player.Position;
            foreach (var pEMP in EMPManager.Current.PersistentEMPs.Values)
            {
                var def = pEMP.ItemToDisable;
                bool shouldAffect = pEMP.IsActive && pEMP.InRange(playerPos);

                if (shouldAffect)
                {
                    if (def.BioTracker)  EMPBioTrackerHandler.Instance?.AddAffectedBy(pEMP);
                    if (def.PlayerFlash) EMPPlayerFlashlightHandler.Instance?.AddAffectedBy(pEMP);
                    if (def.PlayerHUD)   EMPPlayerHudHandler.Instance?.AddAffectedBy(pEMP);
                    if (def.Sentry)      
                        foreach (var s in EMPSentryHandler.AllSentries)
                            s.AddAffectedBy(pEMP);
                    if (def.GunSight)
                        foreach (var h in EMPGunSightHandler.AllGunSights)
                            h.AddAffectedBy(pEMP);
                }
                else
                {
                    if (def.BioTracker)  EMPBioTrackerHandler.Instance?.RemoveAffectedBy(pEMP);
                    if (def.PlayerFlash) EMPPlayerFlashlightHandler.Instance?.RemoveAffectedBy(pEMP);
                    if (def.PlayerHUD)   EMPPlayerHudHandler.Instance?.RemoveAffectedBy(pEMP);
                    if (def.Sentry)
                        foreach (var s in EMPSentryHandler.AllSentries)
                            s.RemoveAffectedBy(pEMP);
                    if (def.GunSight)
                        foreach (var h in EMPGunSightHandler.AllGunSights)
                            h.RemoveAffectedBy(pEMP);
                }
            }
        }                
    }
}
