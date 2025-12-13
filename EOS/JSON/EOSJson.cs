using AmorLib.Dependencies;
using AmorLib.Utils;
using System.Text.Json;

namespace EOS.JSON
{
    public static class EOSJson
    {
        private static readonly JsonSerializerOptions _setting = JsonSerializerUtil.CreateDefaultSettings(true, PData_Wrapper.IsLoaded, InjectLib_Wrapper.IsLoaded);

        static EOSJson()
        {
            _setting.Converters.Add(new MyVector3Converter());
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting)!;
        }

        public static object Deserialize(Type type, string json)
        {
            return JsonSerializer.Deserialize(json, type, _setting)!;
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting);
        }
    }
}
