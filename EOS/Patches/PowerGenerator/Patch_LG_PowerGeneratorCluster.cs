using EOS.Modules.Instances;
using EOS.Modules.Objectives.GeneratorCluster;
using HarmonyLib;
using LevelGeneration;
using UnityEngine;

namespace EOS.Patches.PowerGenerator
{
    [HarmonyPatch(typeof(LG_PowerGeneratorCluster), nameof(LG_PowerGeneratorCluster.Setup))]
    internal static class Patch_LG_PowerGeneratorCluster
    {        
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_PowerGeneratorCluster_Setup(LG_PowerGeneratorCluster __instance)
        {
            GeneratorClusterInstanceManager.Current.Register(__instance);

            if (!GeneratorClusterObjectiveManager.Current.TryGetDefinition(__instance, out var def))
                return;

            EOSLogger.Debug("Found LG_PowerGeneratorCluster and its definition! Building this Generator cluster...");

            // ========== vanilla build =================
            __instance.m_serialNumber = SerialGenerator.GetUniqueSerialNo();
            __instance.m_itemKey = "GENERATOR_CLUSTER_" + __instance.m_serialNumber.ToString();
            __instance.m_terminalItem = GOUtil.GetInterfaceFromComp<iTerminalItem>(__instance.m_terminalItemComp);
            __instance.m_terminalItem.Setup(__instance.m_itemKey);
            __instance.m_terminalItem.FloorItemStatus = eFloorInventoryObjectStatus.UnPowered;
            
            if (__instance.SpawnNode != null)
                __instance.m_terminalItem.FloorItemLocation = __instance.SpawnNode.m_zone.NavInfo.GetFormattedText(LG_NavInfoFormat.Full_And_Number_With_Underscore);

            List<Transform> transformList = new(__instance.m_generatorAligns);
            uint numberOfGenerators = def.NumberOfGenerators;
            __instance.m_generators = new LG_PowerGenerator_Core[numberOfGenerators];
            
            if (transformList.Count >= numberOfGenerators)
            {
                for (int j = 0; j < numberOfGenerators; ++j)
                {
                    int k = Builder.BuildSeedRandom.Range(0, transformList.Count);
                    LG_PowerGenerator_Core generator = GOUtil.SpawnChildAndGetComp<LG_PowerGenerator_Core>(__instance.m_generatorPrefab, transformList[k]);
                    __instance.m_generators[j] = generator;

                    generator.SpawnNode = __instance.SpawnNode;
                    PowerGeneratorInstanceManager.Current.MarkAsGCGenerator(__instance, generator);
                    generator.Setup();
                    generator.SetCanTakePowerCell(true);
                    
                    Debug.Log("Spawning generator at alignIndex: " + k);
                    transformList.RemoveAt(k);
                }
            }
            else
                Debug.LogError("LG_PowerGeneratorCluster does NOT have enough generator aligns to support the warden objective! Has " + transformList.Count + " needs " + numberOfGenerators);

            __instance.ObjectiveItemSolved = true;

            if (def.EndSequenceChainedPuzzle != 0u)
            {
                GeneratorClusterObjectiveManager.Current.RegisterForChainedPuzzleBuild(__instance, def);
            }
        }
    }
}
