using GTFO.API;
using UnityEngine;

namespace EOS.Modules.World.EMP.Handlers
{
    public sealed class EMPGunSightHandler : EMPHandler
    {        
        public static IEnumerable<EMPGunSightHandler> AllGunSights => s_instances;        
        private static readonly List<EMPGunSightHandler> s_instances = new();
        private GameObject[] _sightPictures = null!;

        static EMPGunSightHandler()
        {
            LevelAPI.OnBuildStart += s_instances.Clear;
            LevelAPI.OnLevelCleanup += s_instances.Clear;
        }

        public override void Setup(GameObject gameObject)
        {
            base.Setup(gameObject);

            _sightPictures = GameObject.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterial?.shader?.name.Contains("HolographicSight") == true)
                .Select(r => r.gameObject)
                .ToArray();

            s_instances.Add(this);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            s_instances.Remove(this);
        }

        protected override void DeviceOn()
        {
            SetSightsActive(true);
        }

        protected override void DeviceOff()
        {
            SetSightsActive(false);
        }

        protected override void FlickerDevice()
        {
            SetSightsActive(EMPManager.RandCoin());
        }

        private void SetSightsActive(bool active)
        {
            foreach (var sight in _sightPictures)
            {
                sight?.SetActive(active);
            }
        }
    }
}
