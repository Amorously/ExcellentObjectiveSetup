using ChainedPuzzles;
using EOS.BaseClasses;
using UnityEngine;

namespace EOS.Modules.Expedition.ThermalSights
{
    public sealed partial class TSAManager : GenericDefinitionManager<TSADefinition, TSAManager>
    {
        public const string ZONE = "Zone";
        public const string INTENSITY = "_Intensity";
        public const string BEHIND_WALL_INTENSITY = "_BehindWallIntensity";

        private readonly List<PuzzleVisualWrapper> _puzzleVisuals = new();
        
        internal void RegisterPuzzleVisual(CP_Bioscan_Core core)
        {
            var components = core.gameObject.GetComponentsInChildren<Renderer>(true);
            if (components == null)
                return;

            foreach (var r in components.Where(comp => comp.gameObject.name.Equals(ZONE)))
            {
                var wrapper = new PuzzleVisualWrapper()
                {
                    GO = r.gameObject,
                    Material = r.material,
                    Intensity = r.material.GetFloat(INTENSITY),
                    BehindWallIntensity = r.material.GetFloat(BEHIND_WALL_INTENSITY)
                };
                _puzzleVisuals.Add(wrapper);
            }
        }        
        
        internal void SetPuzzleVisualsIntensity(float t)
        {
            _puzzleVisuals.ForEach(v => v.SetIntensity(t));
        }

        private void CleanupPuzzleVisuals()
        {
            _puzzleVisuals.Clear();
        }
    }
}
