using AmorLib.Utils.JsonElementConverters;
using GTFO.API;
using Localization;

namespace EOS.Utils
{
    public static class TSL_Wrapper
    {
        public static LocalizedText ParseToLocalizedText(this LocaleText text)
        {
            return new() { UntranslatedText = text.ParseTextFragments(), Id = 0u };
        }

        public static string ParseTextFragments(this LocaleText input)
        {
            return ParseTextFragments(input.ToString());
        }
        
        public static string ParseTextFragments(this string input)
        {
            return InteropAPI.Call("TSL.ParseTextFragments", input) as string ?? input;
        }        
    }
}
