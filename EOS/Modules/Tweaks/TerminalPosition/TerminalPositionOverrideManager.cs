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
            if (!TryGetDefinition(globalIndex, instanceIndex, out var posOverride)) // modify terminal position
                return; 

            Vector3 position = posOverride.Position;
            Quaternion rotation = posOverride.Rotation;
            if (position == Vector3.zero) 
                return;

            term.transform.position = position;
            term.transform.rotation = rotation;
            EOSLogger.Debug($"TerminalPositionOverride: {posOverride}");

            var newNode = CourseNodeUtil.GetCourseNode(position, Dimension.GetDimensionFromPos(position).DimensionIndex);
            if (term.SpawnNode.NodeID != newNode.NodeID) // instantiate new prefab and update node
                EOSLogger.Warning($"{DEFINITION_NAME}: terminal in {globalIndex}, {instanceIndex} is being moved to different node");
        }
    }
}
