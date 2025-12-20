using EOS.BaseClasses;
using EOS.Modules.Expedition.Gears;
using GameData;
using GTFO.API;
using GTFO.API.Utilities;
using UnityEngine;

namespace EOS.Modules.Tweaks.ThermalSights
{
    public sealed partial class TSAManager : GenericDefinitionManager<TSADefinition, TSAManager>
    {                
        public const bool OUTPUT_THERMAL_SHADER_SETTINGS_ON_WIELD = false;
        public const string THERMAL = "Thermal";
        public const string ZOOM = "_Zoom";

        protected override string DEFINITION_NAME => "ThermalSight";
    
        public uint CurrentGearPID { get; private set; } = 0u;

        private readonly Dictionary<uint, Renderer[]> _inLevelGearThermals = new();
        private readonly HashSet<uint> _modifiedInLevelGearThermals = new();
        private readonly HashSet<uint> _thermalOfflineGears = new();

        static TSAManager()
        {
            GameDataAPI.OnGameDataInitialized += OnGameDataInitialized;
        }

        private static void OnGameDataInitialized()
        {
            Current.InitThermalOfflineGears();
        }

        protected override void FileChanged(LiveEditEventArgs e)
        {
            base.FileChanged(e);
            InitThermalOfflineGears();
            CleanupInLevelGearThermals(true);
            SetThermalSightRenderer(CurrentGearPID);
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            CurrentGearPID = 0;
            CleanupInLevelGearThermals();
            CleanupPuzzleVisuals();
        }

        public bool IsGearWithThermal(uint gearPID) => _thermalOfflineGears.Contains(gearPID);

        private void InitThermalOfflineGears()
        {
            _thermalOfflineGears.Clear();
            foreach (var block in PlayerOfflineGearDataBlock.GetAllBlocks())
            {
                if (block.internalEnabled && block.name.ToLowerInvariant().EndsWith("_t"))
                {
                    _thermalOfflineGears.Add(block.persistentID);
                }
            }
            EOSLogger.Debug($"Found OfflineGear with thermal sight, count: {_thermalOfflineGears.Count}");
        }

        private void SetThermalSightRenderer(uint gearPID = 0u)
        {
            if (gearPID == 0u)
                gearPID = CurrentGearPID;

            if (!IsGearWithThermal(gearPID) || _modifiedInLevelGearThermals.Contains(gearPID))
                return;
            if (!GenericDefinitions.TryGetValue(gearPID, out var definition) || !_inLevelGearThermals.TryGetValue(gearPID, out var renderers))
                return;            

            var def = definition.Definition;
            var shader = def.Shader;
            foreach (var r in renderers)
            {
                foreach (var prop in shader.GetType().GetProperties())
                {
                    var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var shaderProp = $"_{prop.Name}";
                    if (type == typeof(float))
                    {
                        r.material.SetFloat(shaderProp, (float)prop.GetValue(shader)!);
                    }
                    else if (type == typeof(Color))
                    {
                        var value = (Color)prop.GetValue(shader)!;
                        r.material.SetVector(shaderProp, value);
                    }
                    else if (type == typeof(bool))
                    {
                        var value = (bool)prop.GetValue(shader)!;
                        r.material.SetFloat(shaderProp, value ? 1.0f : 0.0f);
                    }
                    else if (type == typeof(Vec4))
                    {
                        var value = (Vec4)prop.GetValue(shader)!;
                        r.material.SetVector(shaderProp, value.ToVector4());
                    }
                }
            }

            _modifiedInLevelGearThermals.Add(gearPID);
        }

        internal void OnPlayerItemWielded(ItemEquippable item)
        {
            if (item?.GearIDRange == null)
            {
                CurrentGearPID = 0;
                return;
            }

            CurrentGearPID = ExpeditionGearManager.GetOfflineGearPID(item.GearIDRange);
            GetInLevelGearThermalRenderersFromItem(item, CurrentGearPID);
        }

        internal void SetCurrentThermalSightSettings(float t)
        {
            if (!GenericDefinitions.TryGetValue(CurrentGearPID, out var def) || !_inLevelGearThermals.TryGetValue(CurrentGearPID, out var renderers))
                return;

            foreach (var r in renderers)
            {
                float OnAimZoom = def.Definition.Shader.Zoom;
                float OffAimZoom = def.Definition.OffAimPixelZoom;
                float zoom = Mathf.Lerp(OnAimZoom, OffAimZoom, t);
                r.material.SetFloat(ZOOM, zoom);
            }
        }
        
        private void CleanupInLevelGearThermals(bool keepCurrentGear = false)
        {
            if (keepCurrentGear && _inLevelGearThermals.TryGetValue(CurrentGearPID, out var renderers))
            {
                _inLevelGearThermals.Clear();
                _inLevelGearThermals[CurrentGearPID] = renderers;
            }
            else
            {
                _inLevelGearThermals.Clear();
            }

            _modifiedInLevelGearThermals.Clear();
        }

        private void GetInLevelGearThermalRenderersFromItem(ItemEquippable item, uint gearPID)
        {
            if (item?.GearIDRange == null)
                return;

            if (gearPID == 0)
                gearPID = ExpeditionGearManager.GetOfflineGearPID(item.GearIDRange);

            if (gearPID == 0 || !IsGearWithThermal(gearPID))
                return;

            if (!_inLevelGearThermals.TryGetValue(gearPID, out var thermals))
                return;

            try
            {
                _ = thermals[0].gameObject.transform.position;
                return;
            }
            catch
            {
                _modifiedInLevelGearThermals.Remove(gearPID);
            }

            var renderers = item.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.sharedMaterial?.shader?.name?.Contains(THERMAL, StringComparison.OrdinalIgnoreCase) == true)
                .ToArray();
            if (renderers.Length == 0)
            {
                EOSLogger.Debug($"{item.PublicName}: thermal renderer not found");
                return;
            }
            else if (renderers.Length > 1)
            {
                EOSLogger.Warning($"{item.PublicName} contains more than 1 thermal renderer!");
            }

            _inLevelGearThermals[gearPID] = renderers;
        }
    }
}
