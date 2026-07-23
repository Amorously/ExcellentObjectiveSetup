using AmorLib.Utils;
using BepInEx.Unity.IL2CPP;
using EOS.BaseClasses;
using EOS.Modules.Objectives.TerminalUplink;
using GTFO.API.Extensions;
using LevelGeneration;

namespace EOS.Modules.Tweaks.TerminalTweak
{
    public sealed partial class SerialGeneratorManager : BaseManager<SerialGeneratorManager>
    {
        private sealed class ShuffledStepArray
        {
            private readonly string[] _values;
            private readonly int[] _order;
            private int _step = -1;

            public ShuffledStepArray(IEnumerable<string> values)
            {
                _values = values.Distinct().ToArray();
                _order = Enumerable.Range(0, _values.Length).ToArray();
                for (int i = _order.Length - 1; i > 0; i--)
                {
                    int j = _random.Next(0, i + 1);
                    (_order[i], _order[j]) = (_order[j], _order[i]);
                }
            }

            public string Next()
            {
                _step++;
                if (_step >= _order.Length) _step = 0; 
                return _values[_order[_step]];
            }
        }

        public const string AUTOGEN_GUID = "000-the_tavern-AutogenRundown";
        public const string LONGERCODES_GUID = "com.Brandont.LongerCodes";

        protected override string DEFINITION_NAME => string.Empty;

        private static readonly bool _hasPluginConflict = IL2CPPChainloader.Instance.Plugins.ContainsKey(AUTOGEN_GUID) || IL2CPPChainloader.Instance.Plugins.ContainsKey(LONGERCODES_GUID);
        private static System.Random _random = null!;
        private static readonly Dictionary<CodeWordLength, ShuffledStepArray> _codeWords = new();
        private static ShuffledStepArray _prefixes = null!;
        private static ShuffledStepArray _hardPrefixes = null!;

        protected override void OnBuildStart()
        {
            if (_hasPluginConflict)
                EOSLogger.Warning($"Conflicting plugin: \"{AUTOGEN_GUID}\" and/or \"{LONGERCODES_GUID}\". Using default SerialGenerator settings");

            _random = RandomUtil.CreateSessionRandom("EOS_SerialGenerator");
            _codeWords.Clear();
            _codeWords[CodeWordLength.Three] = new(ThreeLetterWords);
            _codeWords[CodeWordLength.Four] = new(FourLetterWords);
            _codeWords[CodeWordLength.Five] = new(FiveLetterWords);
            _codeWords[CodeWordLength.Six] = new(SixLetterWords);
            _codeWords[CodeWordLength.Seven] = new(SevenLetterWords);
            _prefixes = new(CodeWordPrefixes);
            _hardPrefixes = new(HardCodeWordPrefixes);
        }

        public static int GetUniqueSerialNo() => SerialGenerator.GetUniqueSerialNo();

        public static string GetIPAddress(bool useIPv6)
        {
            if (_hasPluginConflict)
                return SerialGenerator.GetIpAddress();

            if (!useIPv6)
            {
                int octet1 = _random.Next(100, 255);
                int octet2 = _random.Next(50, 255);
                int octet3 = _random.Next(0, 255);
                int octet4 = _random.Next(0, 255);
                return $"{octet1}.{octet2}.{octet3}.{octet4}";
            }

            string prefix = Ipv6Prefixes[_random.Next(0, Ipv6Prefixes.Length)];
            var segments = prefix.Split(':').Select(part => part.ToLowerInvariant()).ToList();
            while (segments.Count < 8)
            {
                if (segments.Count is >= 3 and <= 5) // Add zero option
                {
                    segments.Add("0");
                    continue;
                }
                var (min, max) = (segments.Count, _random.Next(0, 10)) switch
                {                    
                    ( >= 6, _) => (0x01000, 0x10000), // Always full for the last 2
                    (_, <= 1) => (0, 0x00000),
                    (_, <= 3) => (0, 0x00100),
                    (_, <= 5) => (0, 0x00200),
                    (_, <= 7) => (0, 0x01000),
                    (_, _) => (0, 0x10000),
                };
                segments.Add(max == 0 ? "0" : _random.Next(min, max).ToString("X"));
            }

            var zeroRanges = new List<(int start, int length)>(); // Identify the longest zero run for compression
            int start = -1;
            for (int i = 0; i <= segments.Count; i++)
            {
                if (i < segments.Count && segments[i] == "0")
                {
                    if (start == -1)
                    {
                        start = i;
                    }
                }
                else
                {
                    if (start != -1)
                    {
                        zeroRanges.Add((start, i - start));
                        start = -1;
                    }
                }
            }

            var bestZeroRun = zeroRanges.OrderByDescending(r => r.length).FirstOrDefault();
            if (bestZeroRun.length < 2) bestZeroRun = default; // Compress only if at least 2
            var parts = new List<string>(); // Rebuild with compression
            for (int j = 0; j < segments.Count;)
            {
                if (bestZeroRun.length >= 2 && j == bestZeroRun.start)
                {
                    parts.Add(""); // :: Compression
                    j += bestZeroRun.length;
                    continue;
                }
                parts.Add(segments[j]);
                j++;
            }
            return string.Join(":", parts.Select(part => part.ToLowerInvariant())).Replace(":::", "::");
        }

        public static int GetCandidateWordsCount(CodeWordLength wordLength)
        {
            if (_hasPluginConflict)
                return 6;

            return wordLength switch
            {
                CodeWordLength.Three => 6,
                CodeWordLength.Four => 6,
                CodeWordLength.Five => 8,
                CodeWordLength.Six => 12,
                CodeWordLength.Seven => 15,
                _ => 6,
            };
        }

        public static string GetCodeWord(CodeWordLength wordLength)
        {
            if (_hasPluginConflict)
                return SerialGenerator.GetCodeWord();

            if (!_codeWords.TryGetValue(wordLength, out var stepArr))
                stepArr = _codeWords[CodeWordLength.Four];
            return stepArr.Next();
        }

        public static string GetCodeWordPrefix(bool useHardPrefixes)
        {
            if (_hasPluginConflict) 
                return SerialGenerator.GetCodeWordPrefix();
            return useHardPrefixes ? _hardPrefixes.Next() : _prefixes.Next();
        }

        public static void SetupUplinkPuzzle(LG_ComputerTerminal terminal, UplinkDefinition def)
        {
            var uplinkPuzzle = terminal.UplinkPuzzle;
            uplinkPuzzle.m_rounds = new List<TerminalUplinkPuzzleRound>().ToIl2Cpp();
            uplinkPuzzle.TerminalUplinkIP = GetIPAddress(def.UseIpv6Addresses);
            uplinkPuzzle.m_roundIndex = 0;
            uplinkPuzzle.m_lastRoundIndexToUpdateGui = -1;
            uplinkPuzzle.m_position = terminal.transform.position;
            uplinkPuzzle.IsCorrupted = def.SetupAsCorruptedUplink && terminal.CorruptedUplinkReceiver != null;
            uplinkPuzzle.m_terminal = terminal;

            uint verificationRounds = Math.Max(def.NumberOfVerificationRounds, 1u);
            int candidateWords = GetCandidateWordsCount(def.CodeWordLength);
            for (int i = 0; i < verificationRounds; i++)
            {
                TerminalUplinkPuzzleRound uplinkPuzzleRound = new()
                {
                    CorrectIndex = _random.Next(0, candidateWords),
                    Prefixes = new string[candidateWords],
                    Codes = new string[candidateWords]
                };
                for (int j = 0; j < candidateWords; j++)
                {
                    uplinkPuzzleRound.Codes[j] = GetCodeWord(def.CodeWordLength);
                    uplinkPuzzleRound.Prefixes[j] = GetCodeWordPrefix(def.UseHardCodeWordPrefixes);
                }
                uplinkPuzzle.m_rounds.Add(uplinkPuzzleRound);
            }
        }

        public static void RerollCorrectIndex(TerminalUplinkPuzzle uplinkPuzzle, int retryCount)
        {
            for (int i = 0; i < retryCount; i++)
            {
                foreach (var uplinkRound in uplinkPuzzle.m_rounds)
                {
                    uplinkRound.CorrectIndex = _random.Next(0, uplinkRound.Codes.Length);
                }
            }
        }
    }
}
