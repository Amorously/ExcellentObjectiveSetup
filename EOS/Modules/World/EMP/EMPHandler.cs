using UnityEngine;

namespace EOS.Modules.World.EMP
{
    public abstract class EMPHandler
    {
        public static IEnumerable<EMPHandler> All => s_handlers.Values;
        private static readonly Dictionary<long, EMPHandler> s_handlers = new();
        private static long s_nextId = 0;

        private readonly HashSet<IEMPSource> _affectedBy = new();
        private long _id;
        protected bool _destroyed;
        private bool _targetOn = true;
        private bool _transitioning;
        private float _flickerStart;
        private float _flickerEnd;
        private bool? _appliedOn;

        protected virtual float FlickerDuration => 0.25f;
        protected virtual float OnToOffMinDelay => 0.0f;
        protected virtual float OnToOffMaxDelay => 0.75f;
        protected virtual float OffToOnMinDelay => 0.5f;
        protected virtual float OffToOnMaxDelay => 1.25f;
        protected virtual bool ContinuouslyEnforce => false;

        public GameObject GameObject { get; private set; } = null!;        
        public Vector3 Position => GameObject?.transform.position ?? Vector3.zero;
        public bool IsTransitioning => _transitioning;        

        public virtual void Setup(GameObject gameObject)
        {            
            GameObject = gameObject;

            foreach (var shock in EMPManager.Current.ActiveShocks)
            {
                if (shock.InRange(Position))
                    AddAffectedBy(shock);
            }

            _id = s_nextId++;
            s_handlers[_id] = this;
        }

        public virtual void OnDespawn()
        {
            _destroyed = true;
            _affectedBy.Clear();
            s_handlers.Remove(_id);            
            GameObject = null!;
        }

        public virtual bool IsEMPed()
        {
            foreach (var src in _affectedBy)
            {
                if (src.IsActive)
                    return true;
            }
            return false;
        }

        public void AddAffectedBy(IEMPSource source) => _affectedBy.Add(source);
        public void RemoveAffectedBy(IEMPSource source) => _affectedBy.Remove(source);
        internal void RemoveInactiveSources() => _affectedBy.RemoveWhere(src => !src.IsActive);

        public void Tick()
        {
            if (_destroyed) return;

            bool shouldBeOn = !IsEMPed();
            float now = Clock.Time;

            if (shouldBeOn != _targetOn)
            {
                _targetOn = shouldBeOn;
                float delay = shouldBeOn ? EMPManager.RandRange(OffToOnMinDelay, OffToOnMaxDelay) : EMPManager.RandRange(OnToOffMinDelay, OnToOffMaxDelay);
                _flickerStart = now + delay;
                _flickerEnd = _flickerStart + FlickerDuration;
                _transitioning = true;
            }

            if (_transitioning)
            {
                if (now < _flickerStart) return;
                if (now < _flickerEnd) 
                { 
                    FlickerDevice(); 
                    return; 
                }
                _transitioning = false;
                ApplyState(_targetOn);
                return;
            }

            if (ContinuouslyEnforce)
                ApplyState(_targetOn, force: true);
        }

        public void ForceState(bool on)
        {
            _targetOn = on;
            _transitioning = false;
            ApplyState(on, force: true);
        }

        private void ApplyState(bool on, bool force = false)
        {
            if (!force && _appliedOn == on) return;
            _appliedOn = on;
            if (on) DeviceOn();
            else DeviceOff();
        }

        protected abstract void DeviceOn();
        protected abstract void DeviceOff();
        protected abstract void FlickerDevice();
    }
}
