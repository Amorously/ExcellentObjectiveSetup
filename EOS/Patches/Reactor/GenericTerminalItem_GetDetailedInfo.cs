using HarmonyLib;
using LevelGeneration;
using Il2CppStringList = Il2CppSystem.Collections.Generic.List<string>;

namespace EOS.Patches.Reactor
{
    // Supplies QUERY details for reactors set up by EOS.
    //
    // Vanilla reactors assign a native Il2Cpp Func<> to m_terminalItem.OnWantDetailedInfo in
    // LG_WardenObjective_Reactor.GenericObjectiveSetup, which produces the "MAIN POWER REACTOR"
    // header in QUERY output. The mod can't do the same: assigning a managed Func<> to that
    // Il2Cpp field crashes the game with AccessViolationException when QUERY triggers the
    // callback (same root cause documented in Patch_LG_PowerGeneratorCluster.cs — "// crash").
    //
    // LG_GenericTerminalItem.GetDetailedInfo returns an empty list when OnWantDetailedInfo is
    // null (it does NOT fall through to defaultDetails), so leaving the field unset means QUERY
    // outputs nothing for our custom reactors. Patch the result here instead — modifying a ref
    // return value doesn't marshal a delegate, so it doesn't trip the same crash.
    [HarmonyPatch(typeof(LG_GenericTerminalItem), nameof(LG_GenericTerminalItem.GetDetailedInfo))]
    internal static class GenericTerminalItem_GetDetailedInfo
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_GetDetailedInfo(
            LG_GenericTerminalItem __instance,
            Il2CppStringList? defaultDetails,
            ref Il2CppStringList? __result)
        {
            if (__result != null && __result.Count > 0)
                return;

            var key = __instance.TerminalItemKey;

            if (key == null || !key.StartsWith("REACTOR_"))
                return;

            var list = new Il2CppStringList();

            list.Add("----------------------------------------------------------------");
            list.Add("MAIN POWER REACTOR");

            if (defaultDetails != null)
                foreach (var detail in defaultDetails)
                    list.Add(detail);

            list.Add("----------------------------------------------------------------");

            __result = list;
        }
    }
}
