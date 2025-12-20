using AmorLib.Utils;
using EOS.BaseClasses;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;

namespace EOS.Modules.Tweaks.SecDoorIntText
{
    public sealed class SecDoorIntTextOverrideManager : ZoneDefinitionManager<SecDoorIntTextOverride, SecDoorIntTextOverrideManager>
    {
        protected override string DEFINITION_NAME => "SecDoorIntText";

        private readonly Dictionary<IntPtr, InteractGlitchComp> _doorLocks  = new();

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _doorLocks.Clear();
        }

        internal void RegisterDoorLocks(LG_SecurityDoor_Locks locks)
        {
            var comp = locks.gameObject.AddComponent<InteractGlitchComp>();
            comp.Init();
            _doorLocks[locks.m_intCustomMessage.Pointer] = comp;
            _doorLocks[locks.m_intOpenDoor.Pointer] = comp;
        }

        public void StartInteractGlitch(Interact_Base interact, bool canInteract = false, bool active = false)
        {
            if (!_doorLocks.TryGetValue(interact.Pointer, out var comp)) 
                return;
            
            if (active)
                comp.CanInteract = canInteract;

            comp.enabled = active;
        }

        public bool TryGetDefinition(LG_SecurityDoor_Locks locks, [MaybeNullWhen(false)] out SecDoorIntTextOverride def)
        {
            var tuple = locks.m_door?.Gate?.m_linksTo?.m_zone?.ToIntTuple() ?? (-1, -1, -1);
            return TryGetDefinition(tuple, out def);
        }
    }
}
