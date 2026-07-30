using AmorLib.Utils;
using BepInEx.Unity.IL2CPP;
using EOS.BaseClasses.CustomTerminalDefinition;
using EOS.Modules.Expedition;
using GTFO.API.Extensions;
using HarmonyLib;
using LevelGeneration;

namespace EOS.Patches.Uplink
{
    [HarmonyPatch]
    internal static class UplinkPuzzleCodesString
    {
        public const string AUTOGEN_GUID = "000-the_tavern-AutogenRundown";
        public const string LONGERCODES_GUID = "com.Brandont.LongerCodes";

        [HarmonyPrepare]
        private static bool PreparePatches() // don't patch if Autogen or LongerCodes is present
        {
            return !IL2CPPChainloader.Instance.Plugins.ContainsKey(AUTOGEN_GUID) && !IL2CPPChainloader.Instance.Plugins.ContainsKey(LONGERCODES_GUID);
        }

        [HarmonyPatch(typeof(TerminalUplinkPuzzle), nameof(TerminalUplinkPuzzle.GetCodesString))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyWrapSafe]
        public static bool TerminalUplinkPuzzle_GetCodesString(ref string __result, TerminalUplinkPuzzleRound round, bool newLine = false) // newLine = isLogfile
        {
            int candidatesCount = round.Codes.Count;
            int wordLength = round.Codes[0].Length;
            bool customWord = candidatesCount > 6 || wordLength > 4;
            bool hyphenated = round.Prefixes[0].Contains('-');
            string delimiter = hyphenated ? " | " : " - ";
            string text = customWord && !newLine ? "\n" : "";

            int perLine = 3; 
            int perLog = 3; 
            if (customWord)
            {
                int entryLength = round.Prefixes[0].Length + 1 + wordLength;  
                int maxPerLine = (68 + 3) / (entryLength + 3);
                int maxPerLog = (42 + 2) / (entryLength + 2);
                perLine = FitEntries(candidatesCount, maxPerLine);
                perLog = FitEntries(candidatesCount, maxPerLog);
            }

            for (int index = 0; index < round.Codes.Length; index++)
            {
                text += $"<color=orange>{round.Prefixes[index]}</color>:{round.Codes[index]}";
                if (index >= round.Codes.Length - 1)
                    continue;
                if (newLine)
                    text += ((index + 1) % perLog) != 0 ? "  " : "\n";
                else
                    text += ((index + 1) % perLine) != 0 ? delimiter : "\n";
            }

            __result = text;
            return false;
        }

        private static int FitEntries(int count, int max)
        {
            if (max < 3)
                return Math.Max(1, max);

            for (int n = Math.Min(max, count - 1); n > 3; n--)
                if (count % n == 0)
                    return n;

            return 3;
        }

        [HarmonyPatch(typeof(LG_TerminalPasswordLinkerJob), nameof(LG_TerminalPasswordLinkerJob.Build))]
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        private static bool Pre_TerminalPasswordLinkerBuild(LG_TerminalPasswordLinkerJob __instance, ref bool __result)
        {
            if (!ExpeditionDefinitionManager.Current.TryGetTerminalDefinitionFromInstance(__instance.m_lockedTerminal, out var def))
                return true;

            var passwordData = new TerminalPasswordData()
            {
                PasswordProtected = true,
                Password = string.Empty,
                PasswordHintText = __instance.m_lockedTerminal.StartStateData?.PasswordHintText ?? "Password Required.",
                GeneratePassword = true,
                PasswordPartCount = __instance.m_passwordParts,
                ShowPasswordLength = __instance.m_showPasswordLength,
                ShowPasswordPartPositions = __instance.m_showPasswordPartPositions,
                PasswordWordLength = def.PasswordWordLength,
                TerminalZoneSelectionDatas = __instance.m_zoneSelectionDatas.ToManaged().Select(list => list.ToManaged().Select(nested => new CustomTerminalZoneSelectionData()
                {
                    DimensionIndex = def.DimensionIndex,
                    Layer = def.Layer,
                    LocalIndex = nested.LocalIndex,
                    Zone = GlobalIndexUtil.TryGetZone(def.DimensionIndex, def.Layer, nested.LocalIndex, out var zone) ? zone : null,
                    IsTemporary = true,
                    SeedType = nested.SeedType,
                    TerminalIndex = nested.TerminalIndex,
                    StaticSeed = nested.StaticSeed
                }).ToList()).ToList() ?? new()
            };

            EOSTerminalUtil.BuildPassword(__instance.m_lockedTerminal, passwordData);
            __result = true;
            return false;
        }
    }
}
