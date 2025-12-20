using AmorLib.Utils.JsonElementConverters;
using EOS.BaseClasses;

namespace EOS.Modules.Tweaks.SecDoorIntText
{
    public enum GlitchMode
    {
        None,
        Style1,
        Style2
    }

    public class SecDoorIntTextOverride : GlobalBased
    {
        public LocaleText Prefix { get; set; } = LocaleText.Empty;

        public LocaleText Postfix { get; set; } = LocaleText.Empty;

        public LocaleText TextToReplace { get; set; } = LocaleText.Empty;

        public GlitchMode GlitchMode { get; set; } = GlitchMode.None;
    }
}
