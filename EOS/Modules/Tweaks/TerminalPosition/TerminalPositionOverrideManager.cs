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

        private static readonly Dictionary<IntPtr, Vector3> s_repositionMap = new();

        static TerminalPositionOverrideManager()
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
                if (!TerminalInstanceManager.Current.TryGetInstance(def.IntTuple, def.InstanceIndex, out var term) || !s_repositionMap.ContainsKey(term.Pointer))
                    continue;
                
                term.transform.SetPositionAndRotation(def.Position, def.Rotation);
                term.m_sound.UpdatePosition(def.Position);
                TryChangeParentNode(term, def.Position);
            }
        }

        protected override void OnBuildStart() => OnLevelCleanup();

        private static void OnBeforeBuildBatch(LG_Factory.BatchName batch)
        {
            if (batch != LG_Factory.BatchName.FunctionMarkerFallback)
                return;

            foreach (var (term, pos) in s_repositionMap)
            {
                TryChangeParentNode(new(term), pos);
            }
        }

        protected override void OnLevelCleanup()
        {
            s_repositionMap.Clear();
        }        

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

            s_repositionMap.Add(term.Pointer, position);
            EOSLogger.Debug($"{DEFINITION_NAME}: modified for {def}");
        }
        
        private static void TryChangeParentNode(LG_ComputerTerminal term, Vector3 pos)
        {
            var courseNode = CourseNodeUtil.GetCourseNode(pos);
            if (courseNode != null && courseNode.NodeID != term.SpawnNode.NodeID)
            {
                term.transform.SetParent(courseNode.m_area.transform, true);
                term.m_terminalItem.SpawnNode = courseNode;
                if (term.SpawnNode.m_zone.ID != courseNode.m_zone.ID)
                    EOSLogger.Warning($"{term.PublicName} from {term.SpawnNode.m_zone.ToIntTuple()} was repositioned to a different zone ({courseNode.m_zone.ToIntTuple()})");
            }
        }
    }
}
