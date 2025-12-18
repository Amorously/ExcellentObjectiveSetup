using GTFO.API;
using GTFO.API.Utilities;
using LevelGeneration;
using MTFO.API;

namespace EOS.BaseClasses
{    
    //BaseManager
    //  ↳ BaseManager<TBase>
    //      ↳ GenericManager<TDef, TBase>
    //          ↳ ManagerClass
    public abstract class BaseManager<TBase> : BaseManager where TBase : BaseManager<TBase>
    {
        public static TBase Current { get; private set; } = null!;
        public BaseManager() => Current = (TBase)this;
    }

    public abstract class BaseManager
    {
        public static uint CurrentMainLevelLayout => RundownManager.ActiveExpedition?.LevelLayoutData ?? 0u;
        public static string MODULE_CUSTOM_FOLDER { get; private set; } = Path.Combine(MTFOPathAPI.CustomPath, "ExtraObjectiveSetup");        
        public string DEFINITION_PATH { get; private set; } = string.Empty;
        protected abstract string DEFINITION_NAME { get; }
        public virtual uint ChainedPuzzleLoadOrder { get; protected set; } = uint.MaxValue;

        private static readonly List<BaseManager> _baseManagers = new();
        private LiveEditListener? _liveEditListener;
        private bool _initialized;

        internal static void SetupManagers(IEnumerable<BaseManager> managers)
        {
            foreach (var manager in managers)
            {                
                _baseManagers.Add(manager);
                manager.Init();
            }

            LevelAPI.OnBuildStart += ManagerBuildStart;
            LevelAPI.OnAfterBuildBatch += ManagerAfterBuildBatch;
            LevelAPI.OnBuildDone += ManagerBuildDone;
            EventAPI.OnExpeditionStarted += ManagerExpeditionStarted;
            LevelAPI.OnEnterLevel += ManagerEnterLevel;
            LevelAPI.OnLevelCleanup += ManagerLevelCleanup;
        }

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            if (DEFINITION_NAME == string.Empty) return;

            if (!Directory.Exists(MODULE_CUSTOM_FOLDER))
            {
                Directory.CreateDirectory(MODULE_CUSTOM_FOLDER);
            }

            DEFINITION_PATH = Path.Combine(MODULE_CUSTOM_FOLDER, DEFINITION_NAME);
            if (!Directory.Exists(DEFINITION_PATH))
            {
                Directory.CreateDirectory(DEFINITION_PATH);
            }
            
            ReadFiles();

            _liveEditListener = LiveEdit.CreateListener(DEFINITION_PATH, "*.json", true);
            _liveEditListener.FileChanged += FileChanged;
        }

        protected virtual void ReadFiles() { }
        protected virtual void FileChanged(LiveEditEventArgs e) { }
        protected virtual void OnBuildStart() { }
        protected virtual void OnAfterBuildBatch(LG_Factory.BatchName batch) { }
        protected virtual void OnBuildDone() { }
        protected virtual void OnExpeditionStarted() { }
        protected virtual void OnEnterLevel() { }
        protected virtual void OnLevelCleanup() { }

        private static void ManagerBuildStart() => _baseManagers.ForEach(manager => manager.OnBuildStart());
        private static void ManagerAfterBuildBatch(LG_Factory.BatchName batch) => _baseManagers.ForEach(manager => manager.OnAfterBuildBatch(batch));
        private static void ManagerBuildDone() => _baseManagers.ForEach(manager => manager.OnBuildDone());
        private static void ManagerExpeditionStarted() => _baseManagers.ForEach(manager => manager.OnExpeditionStarted());
        private static void ManagerEnterLevel() => _baseManagers.ForEach(manager => manager.OnEnterLevel());
        private static void ManagerLevelCleanup() => _baseManagers.ForEach(manager => manager.OnLevelCleanup());
    }
}
