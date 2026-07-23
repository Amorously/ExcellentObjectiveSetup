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
            if (round.Codes.Length == 0)
                return true;
            
            int codeWordLength = round.Codes[0].Length;
            int perLineDisplay = codeWordLength switch
            {
                5 => 4,
                6 => 4,
                7 => 5,
                _ => 3,
            };
            int perLineLogFile = codeWordLength switch
            {
                5 => 4,
                _ => 3,
            };

            string text = !newLine ? "\n" : ""; // newLine = isLogfile
            for (int index = 0; index < round.Codes.Length; index++)
            {
                text += $"<color=orange>{round.Prefixes[index]}</color>:{round.Codes[index]}";
                if (index >= round.Codes.Length - 1)
                    continue;
                if (newLine)
                    text += ((index + 1) % perLineLogFile) != 0 ? "  " : "\n";
                else
                    text += ((index + 1) % perLineDisplay) != 0 ? " - " : "\n";
            }

            __result = text;
            return false;
        }
    }
}
