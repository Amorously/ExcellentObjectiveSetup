using GTFO.API;

namespace EOS
{
    public static class EOSNetworking
    {
        public const uint INVALID_ID = 0u;
        public const uint FOREVER_REPLICATOR_ID_START = 1000u;
        public const uint REPLICATOR_ID_START = 10000u;
        
        private static readonly HashSet<uint> _foreverUsedIDs = new();
        private static readonly HashSet<uint> _usedIDs = new();
        private static uint _currentForeverID = FOREVER_REPLICATOR_ID_START;
        private static uint _currentID = REPLICATOR_ID_START;

        static EOSNetworking()
        {
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
        }

        public static uint AllotReplicatorID()
        {
            while (_currentID >= REPLICATOR_ID_START && _usedIDs.Contains(_currentID)) // prevent overflow
                _currentID += 1;

            if (_currentID < REPLICATOR_ID_START)
            {
                EOSLogger.Error("Replicator ID depleted. How?");
                return INVALID_ID;
            }

            uint allotedID = _currentID;
            _usedIDs.Add(allotedID);
            _currentID += 1;
            return allotedID;
        }

        public static bool TryAllotID(uint id) => _usedIDs.Add(id);

        public static uint AllotForeverReplicatorID()
        {
            while (_currentForeverID < REPLICATOR_ID_START && _foreverUsedIDs.Contains(_currentForeverID))
                _currentForeverID += 1;

            if (_currentForeverID >= REPLICATOR_ID_START)
            {
                EOSLogger.Error("Forever Replicator ID depleted.");
                return INVALID_ID;
            }

            uint allotedID = _currentForeverID;
            _foreverUsedIDs.Add(allotedID);
            _currentForeverID += 1;
            return allotedID;
        }

        private static void Clear()
        {
            _usedIDs.Clear();
            _currentID = REPLICATOR_ID_START;
        }

        public static void ClearForever()
        {
            _foreverUsedIDs.Clear();
            _currentForeverID = FOREVER_REPLICATOR_ID_START;
        }
    }
}