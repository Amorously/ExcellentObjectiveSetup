using AmorLib.Utils;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using LevelGeneration;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EOS.Modules.Objectives.IndividualGenerator
{
    internal sealed class IndividualGeneratorObjectiveManager : InstanceDefinitionManager<IndividualGeneratorDefinition, IndividualGeneratorObjectiveManager>
    {
        protected override string DEFINITION_NAME { get; } = "IndividualGenerator";
        public override uint ChainedPuzzleLoadOrder => 0u;

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
                if (!def.RepositionCover || markerProducer == null)
                {
                    gen.transform.SetPositionAndRotation(position, rotation);
                }
                else
                {
                    for (int i = 0; i < markerProducer.transform.childCount; i++)
                    {
                        markerProducer.transform.GetChild(i).SetPositionAndRotation(position, rotation);
                    }
                }

                var newNode = CourseNodeUtil.GetCourseNode(position, Dimension.GetDimensionFromPos(position).DimensionIndex);
                if (gen.SpawnNode.NodeID != newNode.NodeID)
                    EOSLogger.Warning($"{DEFINITION_NAME}: generator in {def} might have been moved to different node");
                else
                    EOSLogger.Debug($"{DEFINITION_NAME}: modified position / rotation for {def}");
            }           

            gen.SetCanTakePowerCell(def.ForceAllowPowerCellInsertion);
            EOSLogger.Debug($"{DEFINITION_NAME}: overriden, instance {def}");
        }
    }
}
