using GTFO.API;

namespace EOS
{
    public static class EOSNetworking
    {
        public const uint INVALID_ID = 0u;

        public const uint FOREVER_REPLICATOR_ID_START = 1000u;

        public const uint REPLICATOR_ID_START = 10000u;

        private static uint currentForeverID = FOREVER_REPLICATOR_ID_START;

        private static uint currentID = REPLICATOR_ID_START;

        private static HashSet<uint> foreverUsedIDs = new();

        private static HashSet<uint> usedIDs = new();

        static EOSNetworking()
        {
            LevelAPI.OnBuildStart += Clear;
            LevelAPI.OnLevelCleanup += Clear;
        }

        public static uint AllotReplicatorID()
        {
            while (currentID >= REPLICATOR_ID_START // prevent overflow
                && usedIDs.Contains(currentID))
                currentID += 1;

            if (currentID < REPLICATOR_ID_START)
            {
                EOSLogger.Error("Replicator ID depleted. How?");
                return INVALID_ID;
            }

            uint allotedID = currentID;
            usedIDs.Add(allotedID);
            currentID += 1;
            return allotedID;
        }

        public static bool TryAllotID(uint id) => usedIDs.Add(id);

        public static uint AllotForeverReplicatorID()
        {
            while (currentForeverID < REPLICATOR_ID_START
                && foreverUsedIDs.Contains(currentForeverID))
                currentForeverID += 1;

            if (currentForeverID >= REPLICATOR_ID_START)
            {
                EOSLogger.Error("Forever Replicator ID depleted.");
                return INVALID_ID;
            }

            uint allotedID = currentForeverID;
            foreverUsedIDs.Add(allotedID);
            currentForeverID += 1;
            return allotedID;
        }

        private static void Clear()
        {
            usedIDs.Clear();
            currentID = REPLICATOR_ID_START;
        }

        public static void ClearForever()
        {
            foreverUsedIDs.Clear();
            currentForeverID = FOREVER_REPLICATOR_ID_START;
        }
    }
}