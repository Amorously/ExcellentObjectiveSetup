using HarmonyLib;
using LevelGeneration;
using Il2CppStringList = Il2CppSystem.Collections.Generic.List<string>;

namespace EOS.Patches.Reactor
{
    [HarmonyPatch(typeof(LG_GenericTerminalItem), nameof(LG_GenericTerminalItem.GetDetailedInfo))]
    internal static class GenericTerminalItem_GetDetailedInfo // credits: pumba
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        private static void Post_GetDetailedInfo(LG_GenericTerminalItem __instance, Il2CppStringList? defaultDetails, ref Il2CppStringList? __result)
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
            {
                foreach (var detail in defaultDetails)
                {
                    list.Add(detail);
                }
            }

            list.Add("----------------------------------------------------------------");
            __result = list;
        }
    }
}
