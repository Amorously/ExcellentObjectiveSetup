using AK;
using AmorLib.Utils.Extensions;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using LevelGeneration;
using System.Collections;
using UnityEngine;

namespace EOS.Modules.Expedition.IndividualGeneratorGroup
{
    internal sealed class ExpeditionIGGroupManager : BaseManager<ExpeditionIGGroupManager>
    {
        protected override string DEFINITION_NAME => string.Empty;

        private readonly Dictionary<IntPtr, ExpeditionIGGroup> _generatorGroups = new();

        protected override void OnBuildDone() // BuildIGGroupsLogic
        {
            if (!ExpeditionDefinitionManager.Current.TryGetDefinition(CurrentMainLevelLayout, out var expDef) || expDef.GeneratorGroups == null || expDef.GeneratorGroups.Count < 1) 
                return;

            foreach(var generatorGroup in expDef.GeneratorGroups)
            {
                foreach (var gen in GatherIGs(generatorGroup))
                {
                    _generatorGroups[gen.Pointer] = generatorGroup;
                }
            }
        }
        
        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnLevelCleanup()
        {
            _generatorGroups.ForEachValue(groupDef => groupDef.GeneratorInstances.Clear());
            _generatorGroups.Clear();
        }

        private static List<LG_PowerGenerator_Core> GatherIGs(ExpeditionIGGroup IGGroup)
        {
            List<LG_PowerGenerator_Core> result = new();
            foreach (var index in IGGroup.Generators)
            {
                if (!PowerGeneratorInstanceManager.Current.TryGetInstance(index.IntTuple, index.InstanceIndex, out var instance))
                {
                    EOSLogger.Error($"Generator instance not found! Instance index: {index}");
                }
                else
                {
                    result.Add(instance);
                }
            }
            IGGroup.GeneratorInstances = result;
            return result;
        }

        public ExpeditionIGGroup? FindGroupDefOf(LG_PowerGenerator_Core core) => _generatorGroups.TryGetValue(core.Pointer, out var group) ? group : null;

        internal static IEnumerator PlayGroupEndSequence(ExpeditionIGGroup igGroup)
        {
            yield return new WaitForSeconds(4f);

            CellSound.Post(EVENTS.DISTANT_EXPLOSION_SEQUENCE);
            yield return new WaitForSeconds(2f);
            EnvironmentStateManager.AttemptSetExpeditionLightMode(false);
            CellSound.Post(EVENTS.LIGHTS_OFF_GLOBAL);
            yield return new WaitForSeconds(3f);

            for (int g = 0; g < igGroup.GeneratorInstances.Count; ++g)
            {
                igGroup.GeneratorInstances[g].TriggerPowerFailureSequence();
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.3f, 1f));
            }

            yield return new WaitForSeconds(4f);
            EnvironmentStateManager.AttemptSetExpeditionLightMode(true);

            int eventIndex = igGroup.GeneratorInstances.Count - 1;
            if(eventIndex >= 0 && eventIndex < igGroup.EventsOnInsertCell.Count)
            {
                EOSWardenEventManager.ExecuteWardenEvents(igGroup.EventsOnInsertCell[eventIndex]);
            }
        }
    }
}
