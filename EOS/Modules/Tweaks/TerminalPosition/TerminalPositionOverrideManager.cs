using AmorLib.Utils;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using GTFO.API;
using GTFO.API.Utilities;
using LevelGeneration;
using UnityEngine;

namespace EOS.Modules.Tweaks.TerminalPosition
{
    public sealed class TerminalPositionOverrideManager: InstanceDefinitionManager<TerminalPosition, TerminalPositionOverrideManager>
    {
        protected override string DEFINITION_NAME => "TerminalPosition";

        static TerminalPositionOverrideManager()
        {
            LevelAPI.OnBeforeBuildBatch += OnBeforeBuildBatch;
        }

        private static void OnBeforeBuildBatch(LG_Factory.BatchName batch)
        {
            if (batch != LG_Factory.BatchName.FunctionMarkerFallback)
                return;

            foreach (var def in Current.GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                if (!TerminalInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var term) || term.ConnectedReactor != null || def.Position == Vector3.zero)
                    continue; // disallow changing position of reactor terminal
                SetParentCourseNode(term, def);
            }
        }

        protected override void FileChanged(FileEventArgs e)
        {
            base.FileChanged(e);
            if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
                return;

            foreach (var def in GetDefinitionsForLevel(CurrentMainLevelLayout))
            {
                if (!TerminalInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var term) || term.ConnectedReactor != null || def.Position == Vector3.zero)
                    continue;
                RepositionTerminal(term, def);
                SetParentCourseNode(term, def);
            }
        }

        internal void Setup(LG_ComputerTerminal term)
        {
            var (globalIndex, instanceIndex) = TerminalInstanceManager.Current.GetGlobalInstance(term);
            if (!TryGetDefinition(globalIndex, instanceIndex, out var def) || term.ConnectedReactor != null || def.Position == Vector3.zero) // disallow changing position of reactor terminal
                return;

            RepositionTerminal(term, def);
            EOSLogger.Debug($"{DEFINITION_NAME}: modified for {def}");
            if (GameStateManager.CurrentStateName == eGameStateName.Generating && LG_Factory.Current.m_currentBatchName >= LG_Factory.BatchName.FunctionMarkerFallback)
                SetParentCourseNode(term, def);
        }

        private static void RepositionTerminal(LG_ComputerTerminal term, TerminalPosition def)
        {
            term.m_sound.UpdatePosition(def.Position);
            var markerProducer = term.GetComponentInParent<LG_MarkerProducer>();
            if (!def.RepositionCover && !def.HideCover || markerProducer == null)
            {
                term.transform.SetPositionAndRotation(def.Position, def.Rotation);
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
                    markerChild.SetPositionAndRotation(def.Position, def.Rotation);
                }
            }
        }

        private static void SetParentCourseNode(LG_ComputerTerminal term, TerminalPosition def)
        {
            var courseNode = CourseNodeUtil.GetCourseNode(def.Position);
            if (courseNode == null || courseNode.NodeID == term.SpawnNode.NodeID || !def.ReassignSpawnNode)
                return;

            EOSLogger.Debug($"{term.PublicName} from Area_{term.SpawnNode.m_area.m_navInfo.Suffix} was repositioned to Area_{courseNode.m_area.m_navInfo.Suffix}");
            term.transform.SetParent(courseNode.m_area.transform, true);
            term.m_terminalItem.SpawnNode = courseNode;
            if (term.SpawnNode.m_zone.ID != courseNode.m_zone.ID)
                EOSLogger.Warning($"{term.PublicName} from zone {def} was repositioned to a different zone ({courseNode.m_zone.ToIntTuple()})");
        }
    }
}
