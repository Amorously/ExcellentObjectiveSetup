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

        static IndividualGeneratorObjectiveManager()
        {
            LevelAPI.OnBeforeBuildBatch += OnBeforeBuildBatch;
        }

        private static void OnBeforeBuildBatch(LG_Factory.BatchName batch)
        {
            if (batch != LG_Factory.BatchName.FunctionMarkerFallback)
                return;

            foreach (var def in Current.GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                if (!PowerGeneratorInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var gen) || def.Position == Vector3.zero)
                    continue;
                SetParentCourseNode(gen, def);
            }
        }
        
        protected override void FileChanged(FileEventArgs e)
        {
            base.FileChanged(e);
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                return;

            foreach (var def in GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                if (!PowerGeneratorInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var gen) || def.Position == Vector3.zero)
                    continue;
                RepositionGenerator(gen, def);
                SetParentCourseNode(gen, def);
            }
        }

        public bool TryGetDefinition(LG_PowerGenerator_Core instance, [MaybeNullWhen(false)] out IndividualGeneratorDefinition definition)
        {
            var (globalIndex, instanceIndex) = PowerGeneratorInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }

        internal void Setup(LG_PowerGenerator_Core gen)
        {
            if (!TryGetDefinition(gen, out var def))
                return;

            if (def.Position != Vector3.zero)
                RepositionGenerator(gen, def);

            gen.SetCanTakePowerCell(def.ForceAllowPowerCellInsertion);
            EOSLogger.Debug($"{DEFINITION_NAME}: overriden for {def}");
        }

        private static void RepositionGenerator(LG_PowerGenerator_Core gen, IndividualGeneratorDefinition def)
        {
            gen.m_sound.UpdatePosition(def.Position);
            var markerProducer = gen.GetComponentInParent<LG_MarkerProducer>();
            if (!def.RepositionCover && !def.HideCover || markerProducer == null)
            {
                gen.transform.SetPositionAndRotation(def.Position, def.Rotation);
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
                    markerChild.SetPositionAndRotation(def.Position, def.Rotation);
                }
            }
        }

        private static void SetParentCourseNode(LG_PowerGenerator_Core gen, IndividualGeneratorDefinition def)
        {
            var courseNode = CourseNodeUtil.GetCourseNode(def.Position);
            if (courseNode == null || courseNode.NodeID == gen.SpawnNode.NodeID || !def.ReassignSpawnNode)
                return;

            EOSLogger.Debug($"{gen.PublicName} from Area_{gen.SpawnNode.m_area.m_navInfo.Suffix} was repositioned to Area_{courseNode.m_area.m_navInfo.Suffix}");
            gen.transform.SetParent(courseNode.m_area.transform, true);
            gen.m_terminalItem.SpawnNode = courseNode;
            if (gen.SpawnNode.m_zone.ID != courseNode.m_zone.ID)
                EOSLogger.Warning($"{gen.PublicName} from {def} was repositioned to a different zone ({courseNode.m_zone.ToIntTuple()})");
        }
    }
}
