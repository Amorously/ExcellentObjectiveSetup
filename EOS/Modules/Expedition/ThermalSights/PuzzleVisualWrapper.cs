using EOS.Modules.Tweaks.ThermalSights;
using UnityEngine;

namespace EOS.Modules.Expedition.ThermalSights
{
    public class PuzzleVisualWrapper
    {
        public GameObject? GO { get; set; } = null;

        public Material? Material { get; set; } = null;

        public float Intensity { get; set; }

        public float BehindWallIntensity { get; set; }

        public void SetIntensity(float t)
        {
            if (GO?.active != true || Material == null)
                return;

            if (Intensity > 0.0f)
            {
                Material.SetFloat(TSAManager.INTENSITY, Intensity * t);
            }
            if (BehindWallIntensity > 0.0f)
            {
                Material.SetFloat(TSAManager.BEHIND_WALL_INTENSITY, BehindWallIntensity * t);
            }
        }
    }
}
