using AmorLib.Utils;
using EOS.BaseClasses;
using EOS.BaseClasses.CustomTerminalDefinition;
using LevelGeneration;

namespace EOS.Modules.Instances
{
    public sealed class ReactorInstanceManager : InstanceManager<LG_WardenObjective_Reactor, ReactorInstanceManager>
    {
        private readonly HashSet<IntPtr> _startupReactor = new();
        private readonly HashSet<IntPtr> _shutdownReactor = new();

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            foreach (var reactor_ptr in _startupReactor)
            {
                var reactor = new LG_WardenObjective_Reactor(reactor_ptr);
                reactor?.m_sound?.Recycle();
            }

            foreach (var reactor_ptr in _shutdownReactor)
            {
                var reactor = new LG_WardenObjective_Reactor(reactor_ptr);
                reactor?.m_sound?.Recycle();
            }

            _startupReactor.Clear();
            _shutdownReactor.Clear();
            base.OnLevelCleanup();
        }

        public override (int, int, int) GetGlobalIndex(LG_WardenObjective_Reactor instance) => instance.SpawnNode.m_zone.ToIntTuple();

        public void MarkAsStartupReactor(LG_WardenObjective_Reactor reactor)
        {
            if (_shutdownReactor.Contains(reactor.Pointer))
            {
                EOSLogger.Error("Invalid: cannot mark a reactor both as startup and shutdown reactor");
                return;
            }

            _startupReactor.Add(reactor.Pointer);
        }

        public void MarkAsShutdownReactor(LG_WardenObjective_Reactor reactor)
        {
            if (_startupReactor.Contains(reactor.Pointer))
            {
                EOSLogger.Error("Invalid: cannot mark a reactor both as startup and shutdown reactor");
                return;
            }

            _shutdownReactor.Add(reactor.Pointer);
        }

        public bool IsStartupReactor(LG_WardenObjective_Reactor reactor) => _startupReactor.Contains(reactor.Pointer);

        public bool IsShutdownReactor(LG_WardenObjective_Reactor reactor) => _shutdownReactor.Contains(reactor.Pointer);

        public static void SetupReactorTerminal(LG_WardenObjective_Reactor reactor, TerminalDefinition reactorTerminalData)
        {
            if (reactorTerminalData == null) return;
            reactorTerminalData.LocalLogFiles?.ForEach(log => reactor.m_terminal.AddLocalLog(log, true));
            reactorTerminalData.UniqueCommands?.ForEach(cmd => EOSTerminalUtil.AddUniqueCommand(reactor.m_terminal, cmd));
            EOSTerminalUtil.BuildPassword(reactor.m_terminal, reactorTerminalData.PasswordData);
        }

        public static LG_WardenObjective_Reactor? FindVanillaReactor(LG_LayerType layer, int count)
        {
            if (count < 0)
            {
                EOSLogger.Error($"FindVanillaReactor: count should be non-negative, but got {count}!");
                return null!;
            }

            LG_WardenObjective_Reactor reactor = null!;
            int c = count;
            foreach (var kvp in WardenObjectiveManager.Current.m_wardenObjectiveItem)
            {
                if (kvp.Key.Layer != layer)
                    continue;

                reactor = kvp.Value?.TryCast<LG_WardenObjective_Reactor>()!;
                if (reactor == null)
                    continue;

                if (c <= 0)
                {
                    break;
                }
                else
                {
                    reactor = null!; // null out explicitly
                    c--;
                    continue;
                }
            }

            if (reactor == null)
            {
                EOSLogger.Error($"FindVanillaReactor: reactor not found with index(Count) {c} in {layer}!");
            }
            return reactor; // will return null if not found
        }
    }
}
