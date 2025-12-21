using AmorLib.Utils.Extensions;
using ChainedPuzzles;
using EOS.BaseClasses;
using GameData;
using GTFO.API;
using UnityEngine;

namespace EOS.Modules.World.NavigationSpline
{
    public class NavigationalSplineManager : GenericExpeditionDefinitionManager<NavigationalSplineDefinition, NavigationalSplineManager>
    {
        public enum SplineEventType
        {
            ToggleSplineState = 610
        }

        protected override string DEFINITION_NAME => "NavigationalSpline";

        public static GameObject SplineGeneratorGO { get; private set; }

        private readonly Dictionary<string, GameObject> _splineGroups = new();
        private static readonly bool _flag;

        static NavigationalSplineManager()
        {
            EOSWardenEventManager.AddEventDefinition(SplineEventType.ToggleSplineState.ToString(), (uint)SplineEventType.ToggleSplineState, ToggleSplineState);

            try
            {
                SplineGeneratorGO = AssetAPI.GetLoadedAsset<GameObject>("Assets/EOSAssets/LG_ChainedPuzzleSplineGenerator.prefab");
                
                if (SplineGeneratorGO == null)
                    throw new Exception("Failed to load navigation spline prefab!");
            }
            catch (Exception ex)
            {
                _flag = true;
                SplineGeneratorGO = new();
                EOSLogger.Error($"{ex}");
            }
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _splineGroups.ForEachValue(group => GameObject.Destroy(group));
            _splineGroups.Clear();
        }

        protected override void OnBuildDone() // InstantiateSplines
        {
            if (_flag)
                FlagMsg();

            if (!GenericExpDefinitions.TryGetValue(CurrentMainLevelLayout, out var def)) 
                return;

            foreach (var groupDef in def.Definitions)
            {
                if (_splineGroups.ContainsKey(groupDef.WorldEventObjectFilter))
                {
                    EOSLogger.Error($"NavigationalSplineManager: duplicate 'WorldEventObjectFilter': {groupDef.WorldEventObjectFilter}, won't build");
                    continue;
                }

                GameObject go = new($"NavigationalSpline_{groupDef.WorldEventObjectFilter}");
                for (int i = 0; i < groupDef.Splines.Count; i++)
                {
                    var splineDef = groupDef.Splines[i];
                    GameObject splineGO = new($"NavigationalSpline_{groupDef.WorldEventObjectFilter}_{i}");
                    splineGO.transform.SetParent(go.transform);

                    var spline = splineGO.AddComponent<CP_Holopath_Spline>();
                    spline.m_splineGeneratorPrefab = SplineGeneratorGO; // spline.Setup will do the copy-instantiation 
                    spline.Setup(false);
                    spline.GeneratePath(splineDef.From, splineDef.To);

                    if (groupDef.RevealSpeedMulti > 0f)
                    {
                        spline.m_revealSpeed *= groupDef.RevealSpeedMulti;
                    }
                }
                _splineGroups[groupDef.WorldEventObjectFilter] = go;
            }
        }

        private static void ToggleSplineState(WardenObjectiveEventData e)
        {
            if (_flag)
                FlagMsg();

            if (!Current._splineGroups.TryGetValue(e.WorldEventObjectFilter, out var splineGroupGO))
            {
                EOSLogger.Error($"NavigationalSplineManager: cannot find Spline Group with name '{e.WorldEventObjectFilter}");
                return;
            }

            for (int i = 0; i < splineGroupGO.transform.childCount; i++)
            {
                if (!splineGroupGO.transform.GetChild(i).gameObject.TryAndGetComponent<CP_Holopath_Spline>(out var spline))
                    continue;
                
                switch (e.Count)
                {
                    case 0: // activate 
                        spline.SetSplineProgress(0);
                        spline.Reveal(0);
                        break;

                    case 1: // instant reveal
                        spline.SetSplineProgress(0.95f);
                        spline.Reveal(0);
                        break;
                    case 2: // deactivate
                        spline.SetVisible(false);
                        break;
                }
            }
        }

        private static void FlagMsg() => EOSLogger.Error("Failed to load spline GameObject during setup!");
    }
}
