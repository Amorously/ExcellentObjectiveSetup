using GameData;
using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using Localization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EOS.Modules.Tweaks.SecDoorIntText
{
    public class InteractGlitchComp : MonoBehaviour
    {
        public const string HEX_CHARPOOL = "0123456789ABCDEF";
        public const string ERR_CHARPOOL = "$#/-01";
        public const string RICH_TEXT = @"<\/?(b|i|u|s|align|allcaps|alpha|br|color|cspace|font|font-weight|gradient|indent|line-height|line-indent|link|lowercase|margin|mark|mspace|nobr|noparse|page|pos|rotate|size|smallcaps|space|sprite|style|sub|sup|uppercase|voffset|width)(=[^>]*)?>";
        public const string ESCAPE_CHAR = @"[\n\r\t\v\b\\\'""]";
        // okay yes this is entirely overkill, but i didn't think of doing another prefix/postfix until afterwards so the regex is staying

        public LG_SecurityDoor_Locks Locks { get; private set; } = null!;
        public GlitchMode Mode { get; internal set; } = GlitchMode.None;
        public bool CanInteract { get; internal set; } = false;

        private readonly StringBuilder _strBuilder = new();
        private readonly List<(string, bool)> _style2Text = new();
        private System.Random _random = null!;
        private eDoorStatus[] _statusWhitelist = null!;
        private uint _holdTextID;
        private float _timer;

        [HideFromIl2Cpp]
        public void Init(SecDoorIntTextDefinition def)
        {
            Locks = GetComponent<LG_SecurityDoor_Locks>();
            Mode = def.GlitchMode;

            _random = new(Locks.GetInstanceID());
            _statusWhitelist = def.ActiveGlitchStatusWhitelist;
            _holdTextID = TextDataBlock.GetBlockID("InGame.InteractionPrompt.Hold_X");

            if (Mode == GlitchMode.Style2)
            {
                int currentIndex = 0;
                string input = def.Style2Text;
                _style2Text.Add((def.Style2Prefix.ParseTextFragments(), true));
                foreach (Match match in Regex.Matches(def.Style2Text, $"{RICH_TEXT}|{ESCAPE_CHAR}", RegexOptions.IgnoreCase))
                {
                    if (match.Index > currentIndex)
                    {
                        _style2Text.Add((input.Substring(currentIndex, match.Index - currentIndex), false));
                    }

                    _style2Text.Add((match.Value, true));
                    currentIndex = match.Index + match.Length;
                }

                if (currentIndex < input.Length)
                {
                    _style2Text.Add((input.Substring(currentIndex), false));
                }
                _style2Text.Add((def.Style2Postfix.ParseTextFragments(), true));
            }

            enabled = false;
        }

        public void Update()
        {
            if (!enabled || _timer > Clock.Time || GuiManager.InteractionLayer == null || Mode == GlitchMode.None)
                return;
            if (_statusWhitelist.Any() && !_statusWhitelist.Contains(Locks.m_lastStatus))
                return;

            GuiManager.InteractionLayer.SetInteractPrompt
            (
                Mode == GlitchMode.Style1 ? GetFormat1() : GetFormat2(), 
                CanInteract ? Text.Format(_holdTextID, InputMapper.GetBindingName(InputAction.Use)) : string.Empty, 
                ePUIMessageStyle.Default
            );
            GuiManager.InteractionLayer.InteractPromptVisible = true;
            _timer = Clock.Time + (Mode == GlitchMode.Style1 ? 0.05f : 0.075f);
        }

        private string GetFormat1()
        {
            return string.Concat(new string[]
            {
                "<color=red>://Decryption E_RR at: [",
                GetRandomHex(),
                GetRandomHex(),
                "-",
                GetRandomHex(),
                GetRandomHex(),
                "-",
                GetRandomHex(),
                GetRandomHex(),
                "-",
                GetRandomHex(),
                GetRandomHex(),
                "]</color>"
            });
        }
        private string GetRandomHex()
        {
            return string.Format("{0}{1}", HEX_CHARPOOL[_random.Next(0, HEX_CHARPOOL.Length)], HEX_CHARPOOL[_random.Next(0, HEX_CHARPOOL.Length)]);
        }
        
        private string GetFormat2()
        {
            _strBuilder.Clear();
            foreach (var (str, isTag) in _style2Text)
            {
                if (isTag)
                {
                    _strBuilder.Append(str);
                    continue;
                }
                
                foreach (char c in str)
                {
                    if (_random.NextDouble() > 0.009999999776482582 || c == ':')
                    {
                        _strBuilder.Append(c);
                    }
                    else
                    {
                        _strBuilder.Append(ERR_CHARPOOL[_random.Next(0, ERR_CHARPOOL.Length)]);
                    }
                }                
            }
            return _strBuilder.ToString();
        }        
    }
}
