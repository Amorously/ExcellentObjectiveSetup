using Player;
using UnityEngine;

namespace EOS.Modules.World.EMP.Handlers
{
    public class EMPPlayerHudHandler : EMPHandler
    {
        public static EMPPlayerHudHandler Instance { get; private set; } = null!;
        private readonly List<RectTransformComp> _hudElements = new();

        protected override bool ContinuouslyEnforce => true;

        public override void Setup(GameObject gameObject, EMPController controller)
        {
            if (Instance != null)
            {
                EOSLogger.Warning("EMPPlayerHudHandler: re-setup detected, despawning old instance");
                Instance.OnDespawn();
            }

            base.Setup(gameObject, controller);
            _hudElements.Clear();
            _hudElements.Add(GuiManager.PlayerLayer.m_compass);
            _hudElements.Add(GuiManager.PlayerLayer.m_wardenObjective);
            _hudElements.Add(GuiManager.PlayerLayer.Inventory);
            _hudElements.Add(GuiManager.PlayerLayer.m_playerStatus);
            Instance = this;
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            _hudElements.Clear();
            Instance = null!;
        }

        protected override void DeviceOn()
        {
            SetHudActive(true);
            SetNavMarkers(true);
            SetGhostOpacity(true);
        }

        protected override void DeviceOff()
        {
            SetHudActive(false);
            SetNavMarkers(false);
            SetGhostOpacity(false);
        }

        protected override void FlickerDevice()
        {
            bool on = EMPManager.RandCoin();
            SetHudActive(on);
            SetNavMarkers(on);
            SetGhostOpacity(on);
        }

        private void SetHudActive(bool on)
        {
            foreach (var hud in _hudElements)
            {
                hud.gameObject.SetActive(on);
            }
        }

        private void SetNavMarkers(bool on)
        {
            foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            {
                if (agent.IsLocallyOwned) continue;
                agent.NavMarker.SetMarkerVisible(on);
            }
        }

        private void SetGhostOpacity(bool on)
        {
            CellSettingsApply.ApplyPlayerGhostOpacity(on ? CellSettingsManager.SettingsData.HUD.Player_GhostOpacity.Value : 0f);
        }
    }
}
