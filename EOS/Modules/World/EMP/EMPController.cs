using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace EOS.Modules.World.EMP
{
    public class EMPController : MonoBehaviour
    {
        public EMPHandler? Handler { get; private set; }
        private const float CLEANUP_INTERVAL = 1f;
        private const float CHECK_INTERVAL = 0.15f;
        private float _nextCleanupTime;
        private float _nextCheckTime;

        [HideFromIl2Cpp]
        public void AssignHandler(EMPHandler handler)
        {
            if (Handler != null)
            {
                EOSLogger.Warning("EMPController: AssignHandler called when a handler was already assigned");
                return;
            }

            Handler = handler;
            Handler.Setup(gameObject, this);
        }

        public void Update()
        {
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel) return;
            if (Handler == null) return;

            float now = Clock.Time;
            if (Handler.IsTransitioning || now >= _nextCheckTime)
            {
                Handler.Tick();
                if (!Handler.IsTransitioning)
                    _nextCheckTime = now + CHECK_INTERVAL;
            }

            if (now >= _nextCleanupTime)
            {
                Handler.RemoveInactiveSources();
                _nextCleanupTime = now + CLEANUP_INTERVAL;
            }
        }

        public void ForceState(bool on)
        {
            Handler?.ForceState(on);
        }

        public void OnDestroy()
        {
            Handler?.OnDespawn();
        }
    }
}
