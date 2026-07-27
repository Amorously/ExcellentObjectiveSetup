using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch(typeof(TerminalUplinkPuzzle), nameof(TerminalUplinkPuzzle.GetCodesString))]
    internal static class UplinkPuzzleCodesString
    {
        public const string AUTOGEN_GUID = "000-the_tavern-AutogenRundown";
        public const string LONGERCODES_GUID = "com.Brandont.LongerCodes";

        [HarmonyPrepare]
        private static bool PreparePatches() // don't patch if Autogen or LongerCodes is present
        {
            return !IL2CPPChainloader.Instance.Plugins.ContainsKey(AUTOGEN_GUID) && !IL2CPPChainloader.Instance.Plugins.ContainsKey(LONGERCODES_GUID);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyWrapSafe]
        public static bool TerminalUplinkPuzzle_GetCodesString(ref string __result, TerminalUplinkPuzzleRound round, bool newLine = false) 
        {
            bool codeWordLength = round.Codes[0].Length > 4;
            bool hyphenated = round.Prefixes[0].Contains('-');
            string delimiter = hyphenated ? " | " : " - ";
            string text = codeWordLength && !newLine ? "\n" : ""; // newLine = isLogfile

            for (int index = 0; index < round.Codes.Length; index++)
            {
                text += $"<color=orange>{round.Prefixes[index]}</color>:{round.Codes[index]}";
                if (index >= round.Codes.Length - 1)
                    continue;
                if (newLine)
                    text += ((index + 1) % 3) != 0 ? "  " : "\n";
                else
                    text += ((index + 1) % 3) != 0 ? delimiter : "\n";
            }

            __result = text;
            return false;
        }
    }
}
