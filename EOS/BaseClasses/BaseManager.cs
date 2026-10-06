using GTFO.API;
using GTFO.API.Utilities;
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
        private SafeFileSystemWatcher _sfsw = null!;
        private bool _initialized;

        internal static void SetupManagers(IEnumerable<BaseManager> managers)
        {
            foreach (var manager in managers)
            {                
                _baseManagers.Add(manager);
                manager.Init();
            }
        }

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            if (DEFINITION_NAME != string.Empty)
            {
                if (!Directory.Exists(MODULE_CUSTOM_FOLDER))
                    Directory.CreateDirectory(MODULE_CUSTOM_FOLDER);
                 
                DEFINITION_PATH = Path.Combine(MODULE_CUSTOM_FOLDER, DEFINITION_NAME);
                if (!Directory.Exists(DEFINITION_PATH))                
                    Directory.CreateDirectory(DEFINITION_PATH);
                
                ReadFiles();
                _sfsw = SafeFileSystemWatcher.Create(DEFINITION_PATH, new string[] { "*.json" }, true);
                _sfsw.OnChanged += FileChanged;
            }

            LevelAPI.OnBuildStart += OnBuildStart;
            LevelAPI.OnBuildDone += OnBuildDone;
            LevelAPI.OnEnterLevel += OnEnterLevel;
            LevelAPI.OnLevelCleanup += OnLevelCleanup;
        }

        protected virtual void ReadFiles() { }
        protected virtual void FileChanged(FileEventArgs e) { }
        protected virtual void OnBuildStart() { }
        protected virtual void OnBuildDone() { }
        protected virtual void OnEnterLevel() { }
        protected virtual void OnLevelCleanup() { }
    }
}
