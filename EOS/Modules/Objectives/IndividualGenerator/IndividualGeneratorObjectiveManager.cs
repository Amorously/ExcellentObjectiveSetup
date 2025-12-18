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

        public void Setup(LG_PowerGenerator_Core gen)
        {
            if (!TryGetDefinition(gen, out var def))
                return;

            Vector3 position = def.Position.ToVector3();
            Quaternion rotation = def.Rotation.ToQuaternion();
            if (position != Vector3.zero)
            {
                gen.transform.position = position;
                gen.transform.rotation = rotation;
                gen.m_sound.UpdatePosition(position);
                EOSLogger.Debug($"LG_PowerGenerator_Core: modified position / rotation");
                
                var newNode = CourseNodeUtil.GetCourseNode(position, Dimension.GetDimensionFromPos(position).DimensionIndex);
                if (gen.SpawnNode.NodeID != newNode.NodeID) // instantiate new prefab and update node
                    EOSLogger.Warning($"{DEFINITION_NAME}: terminal in {def} is being moved to different node"); 
            }           

            gen.SetCanTakePowerCell(def.ForceAllowPowerCellInsertion);
            EOSLogger.Debug($"LG_PowerGenerator_Core: overriden, instance {def}");
        }
        
        public bool TryGetDefinition(LG_PowerGenerator_Core instance, [MaybeNullWhen(false)] out IndividualGeneratorDefinition definition)
        {
            var (globalIndex, instanceIndex) = PowerGeneratorInstanceManager.Current.GetGlobalInstance(instance);
            return TryGetDefinition(globalIndex, instanceIndex, out definition);
        }
    }
}
