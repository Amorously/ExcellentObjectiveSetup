using AmorLib.Utils;
using GameData;
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
        public (int, int, int) GlobalIndex { get; private set; } = (-1, -1, -1);
        public GlitchMode Mode { get; private set; } = GlitchMode.None;
        public bool CanInteract { get; internal set; } = false;

        private readonly StringBuilder _strBuilder = new();
        private System.Random _random = null!;
        private uint START_SECURITY_SCAN_SEQUENCE_TEXT_ID;
        private uint HOLD_TEXT_ID;
        private uint SCAN_UNKNOWN_TEXT_DB;
        private float _timer;

        public void Init()
        {
            Locks = GetComponent<LG_SecurityDoor>().m_locks.Cast<LG_SecurityDoor_Locks>();
            GlobalIndex = Locks.m_door?.Gate?.m_linksTo?.m_zone?.ToIntTuple() ?? (-1, -1, -1);
            Mode = SecDoorIntTextOverrideManager.Current.GetDefinition(GlobalIndex)?.GlitchMode ?? GlitchMode.None;
            _random = new(Locks.GetInstanceID());

            START_SECURITY_SCAN_SEQUENCE_TEXT_ID = TextDataBlock.GetBlock("InGame.InteractionPrompt.SecurityDoor.StartSecurityScanSequence")?.persistentID ?? 0u;
            HOLD_TEXT_ID = TextDataBlock.GetBlock("InGame.InteractionPrompt.Hold_X")?.persistentID ?? 0u;
            SCAN_UNKNOWN_TEXT_DB = TextDataBlock.GetBlock("InGame.InteractionPrompt.SecurityDoor.StartSecurityScanSequence_ScanUnknown")?.persistentID ?? 0u;

            enabled = false;
        }

        public void Update()
        {
            if (!enabled || _timer > Clock.Time || GuiManager.InteractionLayer == null || Mode == GlitchMode.None)
                return;

            switch (Mode)
            {
                case GlitchMode.Style1:
                    GuiManager.InteractionLayer.SetInteractPrompt(GetFormat1(), CanInteract ? Text.Format(HOLD_TEXT_ID, InputMapper.GetBindingName(InputAction.Use)) : string.Empty, ePUIMessageStyle.Default);
                    GuiManager.InteractionLayer.InteractPromptVisible = true;
                    _timer = Clock.Time + 0.05f;
                    break;

                case GlitchMode.Style2:
                    string format = GetFormat2(Text.Get(START_SECURITY_SCAN_SEQUENCE_TEXT_ID));
                    string format2 = GetFormat2(Text.Get(SCAN_UNKNOWN_TEXT_DB));
                    GuiManager.InteractionLayer.SetInteractPrompt($"{format}<color=red>{format2}</color>", CanInteract ? Text.Format(HOLD_TEXT_ID, InputMapper.GetBindingName(InputAction.Use)) : string.Empty, ePUIMessageStyle.Default);
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
