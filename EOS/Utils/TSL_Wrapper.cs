using AmorLib.Utils.JsonElementConverters;
using GTFO.API;

namespace EOS.Utils
{
    public static class TSL_Wrapper
    {
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
