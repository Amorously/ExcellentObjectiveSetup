using AmorLib.Utils.JsonElementConverters;
using EOS.BaseClasses;
using LevelGeneration;

namespace EOS.Modules.Tweaks.SecDoorIntText
{
    public enum GlitchMode
    {
        None,
        Style1,
        Style2
    }

    public class SecDoorIntTextDefinition : GlobalBased
    {
        public LocaleText Prefix { get; set; } = LocaleText.Empty;

        public LocaleText Postfix { get; set; } = LocaleText.Empty;

        public LocaleText TextToReplace { get; set; } = LocaleText.Empty;

        public eDoorStatus[] ActiveTextOverrideWhitelist { get; set; } = Array.Empty<eDoorStatus>();

        public GlitchMode GlitchMode { get; set; } = GlitchMode.None;

        public LocaleText Style2Prefix { get; set; } = LocaleText.Empty;

        public LocaleText Style2Postfix { get; set; } = LocaleText.Empty;

        public LocaleText Style2Text { get; set; } = LocaleText.Empty;

        public eDoorStatus[] ActiveGlitchStatusWhitelist { get; set; } = Array.Empty<eDoorStatus>();
    }
}
