global using EOS.Utils;
using AmorLib.Dependencies;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using EOS.BaseClasses;
using EOS.Modules.Objectives.Reactor;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using System.Reflection;

namespace EOS
{
    [BepInPlugin("Amor.ExcellentObjectiveSetup", "ExcellentObjectiveSetup", "10.0.0")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.dak.MTFO", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("Amor.AmorLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(InjectLib_Wrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(PData_Wrapper.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.sinai.UnityExplorer", BepInDependency.DependencyFlags.SoftDependency)] // fix CTD
    [BepInIncompatibility("Inas.ExtraObjectiveSetup")]
    internal sealed class EntryPoint : BasePlugin
    {
        private readonly List<Type[]> _callbackAssemblyTypes = new() { AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()) };

        public override void Load()
        {
            new Harmony("ExcellentObjectiveSetup").PatchAll();

            InteropAPI.RegisterCall("EOS_Managers", args =>
            {
                if (args?.Length > 0 && args[0] is Type[] types)
                {
                    _callbackAssemblyTypes.Add(types);
                }
                return null;
            });

            ClassInjector.RegisterTypeInIl2Cpp<OverrideReactorComp>();

            AssetAPI.OnStartupAssetsLoaded += SetupManagers;
            EOSLogger.Log("EOS is done loading!");
        }

        private void SetupManagers()
        {
            var managers = _callbackAssemblyTypes
                .SelectMany(types => types)
                .Where(t => typeof(BaseManager).IsAssignableFrom(t) && !t.IsAbstract)
                .Select(t => (BaseManager)Activator.CreateInstance(t, true)!);
            BaseManager.SetupManagers(managers);
        }
    }
}
