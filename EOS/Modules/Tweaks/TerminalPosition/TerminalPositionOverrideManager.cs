using AmorLib.Utils.Extensions;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using LevelGeneration;
using UnityEngine;

namespace EOS.Modules.Tweaks.TerminalPosition
{
    internal sealed class TerminalPositionOverrideManager: InstanceDefinitionManager<TerminalPosition, TerminalPositionOverrideManager>
    {
        protected override string DEFINITION_NAME => "TerminalPosition";

        public void Setup(LG_ComputerTerminal term)
        {
            if (term.ConnectedReactor != null) // disallow changing position of reactor terminal
                return; 

            var (globalIndex, instanceIndex) = TerminalInstanceManager.Current.GetGlobalInstance(term);
            if (!TryGetDefinition(globalIndex, instanceIndex, out var def)) // modify terminal position
                return; 

            Vector3 position = def.Position;
            Quaternion rotation = def.Rotation;
            if (position == Vector3.zero) 
                return;

            term.m_sound.UpdatePosition(position);

            var markerProducer = term.GetComponentInParent<LG_MarkerProducer>();
            if (!def.RepositionCover && !def.HideCover || markerProducer == null)
            {
                term.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                var markerTransform = markerProducer.transform;
                while (markerTransform.childCount == 1 && markerTransform.GetChild(0).GetComponent<LG_ComputerTerminal>() == null)
                {
                    markerTransform = markerTransform.GetChild(0);
                }
                for (int i = 0; i < markerTransform.childCount; i++)
                {
                    var markerChild = markerTransform.GetChild(i); 
                    var childTerm = markerChild.GetComponentInChildren<LG_ComputerTerminal>(true);
                    if (def.HideCover && childTerm == null)
                    {
                        markerChild.gameObject.SetActive(false);
                        continue;
                    }
                    markerChild.SetPositionAndRotation(position, rotation);
                }
            }

            EOSLogger.Debug($"{DEFINITION_NAME}: modified for {def}");
        }
    }
}
