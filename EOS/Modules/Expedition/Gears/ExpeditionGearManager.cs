using EOS.BaseClasses;
using Gear;
using Player;
using System.Collections.Immutable;

namespace EOS.Modules.Expedition.Gears
{
    public sealed class ExpeditionGearManager : BaseManager<ExpeditionGearManager>
    {
        protected override string DEFINITION_NAME => string.Empty;

        public GearManager VanillaGearManager { get; internal set; } = null!; // setup in patch: GearManager.LoadOfflineGearDatas        
        public ImmutableDictionary<InventorySlot, Dictionary<uint, GearIDRange>> GearSlots { get; } = ImmutableDictionary.CreateRange(new KeyValuePair<InventorySlot, Dictionary<uint, GearIDRange>>[]
        {
            new(InventorySlot.GearStandard, new()),
            new(InventorySlot.GearSpecial, new()),
            new(InventorySlot.GearMelee, new()),
            new(InventorySlot.GearClass, new())
        });
        
        private readonly HashSet<uint> _gearIds = new();
        private Mode _mode = Mode.DISALLOW;

        private void ClearLoadedGears()
        {
            foreach (int inventorySlot in GearSlots.Select(kvp => (int)kvp.Key))
            {
                VanillaGearManager.m_gearPerSlot[inventorySlot].Clear();
            }
        }

        private bool IsGearAllowed(uint playerOfflineGearDBPID)
        {
            switch (_mode)
            {
                case Mode.ALLOW: 
                    return _gearIds.Contains(playerOfflineGearDBPID);

                case Mode.DISALLOW: 
                    return !_gearIds.Contains(playerOfflineGearDBPID);

                default:
                    EOSLogger.Error($"Unimplemented Mode: {_mode}, will allow gears anyway...");
                    return true;
            }
        }

        private void AddGearForCurrentExpedition()
        {
            foreach (var (inventorySlot, loadedGears) in GearSlots)
            {
                var vanillaSlot = VanillaGearManager.m_gearPerSlot[(int)inventorySlot];
                var loadedGearsInCategory = loadedGears;

                if (loadedGearsInCategory.Count == 0)
                {
                    EOSLogger.Debug($"No gear has been loaded for {inventorySlot}.");
                    continue;
                }

                foreach (uint offlineGearPID in loadedGearsInCategory.Keys)
                {
                    if (IsGearAllowed(offlineGearPID))
                    {
                        vanillaSlot.Add(loadedGearsInCategory[offlineGearPID]);
                    }
                }

                if (vanillaSlot.Count == 0)
                {
                    EOSLogger.Error($"No gear is allowed for {inventorySlot}, there must be at least 1 allowed gear!");
                    vanillaSlot.Add(loadedGearsInCategory.First().Value);
                }
            }
        }

        private void ResetPlayerSelectedGears()
        {
            VanillaGearManager.RescanFavorites();
            foreach (int inventorySlot in GearSlots.Select(kvp => (int)kvp.Key))
            {
                try
                {
                    if (VanillaGearManager.m_lastEquippedGearPerSlot[inventorySlot] != null)
                        PlayerBackpackManager.EquipLocalGear(VanillaGearManager.m_lastEquippedGearPerSlot[inventorySlot]);
                    else if (VanillaGearManager.m_favoriteGearPerSlot[inventorySlot].Count > 0)
                        PlayerBackpackManager.EquipLocalGear(VanillaGearManager.m_favoriteGearPerSlot[inventorySlot][0]);
                    else if (VanillaGearManager.m_gearPerSlot[inventorySlot].Count > 0)
                        PlayerBackpackManager.EquipLocalGear(VanillaGearManager.m_gearPerSlot[inventorySlot][0]);
                }
                catch (Il2CppInterop.Runtime.Il2CppException e)
                {
                    EOSLogger.Error($"Error attempting to equip gear for slot {inventorySlot}:\n{e.StackTrace}");
                }
            }
        }

        private void ConfigExpeditionGears()
        {
            _mode = Mode.DISALLOW;
            _gearIds.Clear();

            if (!ExpeditionDefinitionManager.Current.TryGetDefinition(CurrentMainLevelLayout, out var expDef) || expDef.ExpeditionGears == null)
                return;

            _mode = expDef.ExpeditionGears.Mode;
            expDef.ExpeditionGears.GearIds.ForEach(id => _gearIds.Add(id));
        }

        internal void SetupAllowedGearsForActiveExpedition()
        {
            ConfigExpeditionGears();
            ClearLoadedGears();
            AddGearForCurrentExpedition();
            ResetPlayerSelectedGears();
        }

        public static uint GetOfflineGearPID(GearIDRange gearIDRange)
        {
            string itemInstanceId = gearIDRange.PlayfabItemInstanceId;
            if (!itemInstanceId.Contains("OfflineGear_ID_"))
            {
                EOSLogger.Error($"Find PlayfabItemInstanceId without substring 'OfflineGear_ID_'! {itemInstanceId}");
                return 0u;
            }

            try
            {
                uint offlineGearPersistentID = uint.Parse(itemInstanceId.Substring("OfflineGear_ID_".Length));
                return offlineGearPersistentID;
            }
            catch
            {
                EOSLogger.Error("Caught exception while trying to parse persistentID of PlayerOfflineGearDB from GearIDRange, which means itemInstanceId could be ill-formated");
                return 0u;
            }
        }
    }

}
