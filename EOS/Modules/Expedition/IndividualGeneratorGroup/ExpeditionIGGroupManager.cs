using AK;
using EOS.BaseClasses;
using EOS.Modules.Instances;
using LevelGeneration;
using System.Collections;

namespace EOS.Modules.Expedition.IndividualGeneratorGroup
{
    internal sealed class ExpeditionIGGroupManager : BaseManager
    {
        protected override string DEFINITION_NAME => string.Empty;

        public static ExpeditionIGGroupManager Current { get; private set; } = new();

        private readonly List<(HashSet<IntPtr> group, ExpeditionIGGroup groupDef)> generatorGroups = new();

        protected override void OnBuildStart() => OnLevelCleanup();

        protected override void OnBuildDone() // BuildIGGroupsLogic
        {
            if (!ExpeditionDefinitionManager.Current.TryGetDefinition(CurrentMainLevelLayout, out var expDef) || expDef.GeneratorGroups == null || expDef.GeneratorGroups.Count < 1) 
                return;

            foreach(var generatorGroup in expDef.GeneratorGroups)
            {
                var generators = GatherIGs(generatorGroup);
                generatorGroups.Add((generators.ConvertAll(new Converter<LG_PowerGenerator_Core, IntPtr>(core => core.Pointer)).ToHashSet(), generatorGroup));
            }
        }

        protected override void OnLevelCleanup()
        {
            foreach (var generatorGroup in generatorGroups)
            {
                generatorGroup.groupDef.GeneratorInstances.Clear();
            }

            generatorGroups.Clear();
        }

        private List<LG_PowerGenerator_Core> GatherIGs(ExpeditionIGGroup IGGroup)
        {
            List<LG_PowerGenerator_Core> result = new();
            IGGroup.Generators.ForEach(index => 
            {
                var instance = PowerGeneratorInstanceManager.Current.GetInstance(index.IntTuple, index.InstanceIndex);
                if(instance == null)
                {
                    EOSLogger.Error($"generator instance not found! Instance index: {index}");
                }
                else
                {
                    result.Add(instance);
                }
            });

            IGGroup.GeneratorInstances = result;
            return result;
        }

        public ExpeditionIGGroup FindGroupDefOf(LG_PowerGenerator_Core core)
        {
            foreach(var (group, groupDef) in generatorGroups)
            {
                if(group.Contains(core.Pointer))
                {
                    return groupDef;
                }
            }
            return null!;
        }

        internal static IEnumerator PlayGroupEndSequence(ExpeditionIGGroup IGGroup)
        {
            yield return new UnityEngine.WaitForSeconds(4f);

            CellSound.Post(EVENTS.DISTANT_EXPLOSION_SEQUENCE);
            yield return new UnityEngine.WaitForSeconds(2f);
            EnvironmentStateManager.AttemptSetExpeditionLightMode(false);
            CellSound.Post(EVENTS.LIGHTS_OFF_GLOBAL);
            yield return new UnityEngine.WaitForSeconds(3f);

            for (int g = 0; g < IGGroup.GeneratorInstances.Count; ++g)
            {
                IGGroup.GeneratorInstances[g].TriggerPowerFailureSequence();
                yield return new UnityEngine.WaitForSeconds(UnityEngine.Random.Range(0.3f, 1f));
            }

            yield return new UnityEngine.WaitForSeconds(4f);
            EnvironmentStateManager.AttemptSetExpeditionLightMode(true);

            int eventIndex = IGGroup.GeneratorInstances.Count - 1;
            if(eventIndex >= 0 && eventIndex < IGGroup.EventsOnInsertCell.Count)
            {
                EOSWardenEventManager.ExecuteWardenEvents(IGGroup.EventsOnInsertCell[eventIndex]);
            }
        }
    }
}
