using AmorLib.Utils;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GTFO.API;
using GTFO.API.Utilities;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EOS.Modules.Objectives.IndividualGenerator
{
    public sealed class IndividualGeneratorObjectiveManager : InstanceDefinitionManager<IndividualGeneratorDefinition, IndividualGeneratorObjectiveManager>
    {
        protected override string DEFINITION_NAME { get; } = "IndividualGenerator";
        public override uint ChainedPuzzleLoadOrder => 0u;

        private static readonly Dictionary<IntPtr, Vector3> s_repositionMap = new();

        static IndividualGeneratorObjectiveManager()
        {
            LevelAPI.OnBeforeBuildBatch += OnBeforeBuildBatch;
        }

        protected override void FileChanged(FileEventArgs e)
        {
            base.FileChanged(e);
            if (!InstanceDefinitions.TryGetValue(CurrentMainLevelLayout, out var defs) || GameStateManager.CurrentStateName != eGameStateName.InLevel)
                return;

            foreach (var def in defs.Definitions)
            {
                if (!PowerGeneratorInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var gen) || !s_repositionMap.ContainsKey(gen.Pointer))
                    continue;

                gen.transform.SetPositionAndRotation(def.Position, def.Rotation);
                gen.m_sound.UpdatePosition(def.Position);
                TryChangeParentNode(gen, def.Position);
            }
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        private static void OnBeforeBuildBatch(LG_Factory.BatchName batch)
        {
            if (batch != LG_Factory.BatchName.FunctionMarkerFallback)
                return;

            foreach (var (gen, pos) in s_repositionMap)
            {
                TryChangeParentNode(new(gen), pos);
            }
        }

        protected override void OnLevelCleanup()
        {
            s_repositionMap.Clear();
        }

        public bool TryGetDefinition(LG_PowerGenerator_Core instance, [MaybeNullWhen(false)] out IndividualGeneratorDefinition definition)
        {
            var (globalIndex, instanceIndex) = PowerGeneratorInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }

        public void Setup(LG_PowerGenerator_Core gen)
        {
            if (!TryGetDefinition(gen, out var def))
                return;

            Vector3 position = def.Position;
            Quaternion rotation = def.Rotation;
            if (position != Vector3.zero)
            {
                gen.m_sound.UpdatePosition(position);     

                var markerProducer = gen.GetComponentInParent<LG_MarkerProducer>();
                if (!def.RepositionCover && !def.HideCover || markerProducer == null)
                {
                    gen.transform.SetPositionAndRotation(position, rotation);
                }
                else
                {
                    var markerTransform = markerProducer.transform;
                    while (markerTransform.childCount == 1 && markerTransform.GetChild(0).GetComponent<LG_PowerGenerator_Core>() == null)
                    {
                        markerTransform = markerTransform.GetChild(0);
                    }
                    for (int i = 0; i < markerTransform.childCount; i++)
                    {
                        var markerChild = markerTransform.GetChild(i);
                        var childGen = markerChild.GetComponentInChildren<LG_PowerGenerator_Core>(true);
                        if (def.HideCover && childGen == null)
                        {
                            markerChild.gameObject.SetActive(false);
                            continue;
                        }
                        markerChild.SetPositionAndRotation(position, rotation);
                    }
                }
            }

            gen.SetCanTakePowerCell(def.ForceAllowPowerCellInsertion);
            s_repositionMap.Add(gen.Pointer, position);
            EOSLogger.Debug($"{DEFINITION_NAME}: overriden, instance {def}");
        }

        private static void TryChangeParentNode(LG_PowerGenerator_Core gen, Vector3 pos)
        {
            var courseNode = CourseNodeUtil.GetCourseNode(pos);
            if (courseNode != null && courseNode.NodeID != gen.SpawnNode.NodeID)
            {
                gen.transform.SetParent(courseNode.m_area.transform, true);
                gen.m_terminalItem.SpawnNode = courseNode;
                if (gen.SpawnNode.m_zone.ID != courseNode.m_zone.ID)
                    EOSLogger.Warning($"{gen.PublicName} from {gen.SpawnNode.m_zone.ToIntTuple()} was repositioned to a different zone ({courseNode.m_zone.ToIntTuple()})");
            }
        }
    }
}
