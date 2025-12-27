using AmorLib.Utils;
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
            if (!def.RepositionCover || markerProducer == null)
            {
                term.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {               
                for (int i = 0; i < markerProducer.transform.childCount; i++)
                {
                    markerProducer.transform.GetChild(i).SetPositionAndRotation(position, rotation);
                }
            }

            var newNode = CourseNodeUtil.GetCourseNode(position, Dimension.GetDimensionFromPos(position).DimensionIndex);
            if (term.SpawnNode.m_searchID != newNode.m_searchID)
                EOSLogger.Warning($"{DEFINITION_NAME}: terminal in {def} might have been moved to different node");
            else
                EOSLogger.Debug($"{DEFINITION_NAME}: modified for {def}");
        }
    }
}
