using GameData;
using Il2CppInterop.Runtime.Attributes;
using LevelGeneration;
using Localization;
using System.Text;
using UnityEngine;

namespace EOS.Modules.Tweaks.SecDoorIntText
{
    public class InteractGlitchComp : MonoBehaviour // CONST_InteractGlitchManager
    {
        public const string HEX_CHARPOOL = "0123456789ABCDEF";
        public const string ERR_CHARPOOL = "$#/-01";

        public LG_SecurityDoor_Locks Locks { get; private set; } = null!;
        public GlitchMode Mode { get; internal set; } = GlitchMode.None;
        public bool CanInteract { get; internal set; } = false;

        private readonly StringBuilder _strBuilder = new();
        private System.Random _random = null!;
        private eDoorStatus[] _statusWhitelist = null!;
        private string _style2Text = string.Empty;
        private string _style2ColoredText = string.Empty;
        private string _htmlColor = string.Empty;
        private uint _holdTextID;
        private float _timer;

        [HideFromIl2Cpp]
        public void Init(SecDoorIntTextDefinition def)
        {
            Locks = GetComponent<LG_SecurityDoor_Locks>();
            Mode = def.GlitchMode;

            _random = new(Locks.GetInstanceID());
            _statusWhitelist = def.ActiveGlitchStatusWhitelist;
            _style2Text = def.Style2Text;
            _style2ColoredText = def.Style2ColoredText;
            _htmlColor = ColorUtility.ToHtmlStringRGB(def.Style2Color);
            _holdTextID = TextDataBlock.GetBlockID("InGame.InteractionPrompt.Hold_X");

            enabled = false;
        }

        public void Update()
        {
            if (!enabled || _timer > Clock.Time || GuiManager.InteractionLayer == null || Mode == GlitchMode.None)
                return;
            if (_statusWhitelist.Any() && !_statusWhitelist.Contains(Locks.m_lastStatus))
                return;

            switch (Mode)
            {
                case GlitchMode.Style1:
                    GuiManager.InteractionLayer.SetInteractPrompt(GetFormat1(), CanInteract ? Text.Format(_holdTextID, InputMapper.GetBindingName(InputAction.Use)) : string.Empty, ePUIMessageStyle.Default);
                    GuiManager.InteractionLayer.InteractPromptVisible = true;
                    _timer = Clock.Time + 0.05f;
                    break;

                case GlitchMode.Style2:
                    string format = $"{GetFormat2(_style2Text)}<color=#{_htmlColor}>{GetFormat2(_style2ColoredText)}</color>";
                    GuiManager.InteractionLayer.SetInteractPrompt(format, CanInteract ? Text.Format(_holdTextID, InputMapper.GetBindingName(InputAction.Use)) : string.Empty, ePUIMessageStyle.Default);
                    GuiManager.InteractionLayer.InteractPromptVisible = true;
                    _timer = Clock.Time + 0.075f;
                    break;
            }
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

        private string GetFormat2(string baseMessage)
        {
            _strBuilder.Clear();
            foreach (char c in baseMessage)
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
            return _strBuilder.ToString();
        }

        private string GetRandomHex()
        {
            return string.Format("{0}{1}", HEX_CHARPOOL[_random.Next(0, HEX_CHARPOOL.Length)], HEX_CHARPOOL[_random.Next(0, HEX_CHARPOOL.Length)]);
        }
    }
}
