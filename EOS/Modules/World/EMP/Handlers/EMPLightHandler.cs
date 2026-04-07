using AmorLib.API;
using GTFO.API;
using LevelGeneration;
using UnityEngine;

namespace EOS.Modules.World.EMP.Handlers
{
    public class EMPLightHandler : EMPHandler
    {
        public static IEnumerable<EMPLightHandler> AllLights => s_instances.Values;        
        private static readonly Dictionary<IntPtr, EMPLightHandler> s_instances = new();
        private readonly LightWorker _worker;
        private readonly LG_Light _light;
        private ILightModifier? _mod;

        static EMPLightHandler()
        {
            LevelAPI.OnBuildStart += s_instances.Clear;
            LevelAPI.OnLevelCleanup += s_instances.Clear;
        }

        public EMPLightHandler(LightWorker worker)
        {
            _worker = worker;
            _light = worker.Light;
        }

        public override void Setup(GameObject gameObject, EMPController controller)
        {
            base.Setup(gameObject, controller);
            s_instances[_light.Pointer] = this;
        }

        protected override void DeviceOn()
        {
            if (_worker == null) return;
            _mod?.Remove();
            _mod = null;
        }

        protected override void DeviceOff()
        {
            if (_worker == null) return;
            _mod ??= _worker.AddModifier(_worker.CurrentColor, _worker.CurrentIntensity, _worker.CurrentEnabled, LightPriority.EMP);            
            _mod.Color = Color.black;
            _mod.Intensity = 0f;
        }

        protected override void FlickerDevice()
        {
            if (_worker == null) return;
            _mod ??= _worker.AddModifier(_worker.CurrentColor, _worker.CurrentIntensity, _worker.CurrentEnabled, LightPriority.EMP);
            _mod.Intensity = EMPManager.Rand.NextSingle() * _worker.OrigIntensity;
        }
    }
}
