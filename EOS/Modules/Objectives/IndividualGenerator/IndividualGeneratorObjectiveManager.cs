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
            EOSLogger.Debug($"{DEFINITION_NAME}: overriden, instance {def}");
        }
    }
}
