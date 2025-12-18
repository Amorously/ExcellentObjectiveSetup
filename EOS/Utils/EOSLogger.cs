using BepInEx.Logging;

namespace EOS.Utils
{
    public static class EOSLogger
    {
        private static readonly ManualLogSource _logger = Logger.CreateLogSource("EOS");

        public static void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }

        public static void Log(string str)
        {
            if (_logger == null) return;

            _logger.Log(LogLevel.Message, str);
        }

        public static void Warning(string format, params object[] args)
        {
            Warning(string.Format(format, args));
        }

        public static void Warning(string str)
        {
            if (_logger == null) return;

            _logger.Log(LogLevel.Warning, str);
        }

        public static void Error(string format, params object[] args)
        {
            Error(string.Format(format, args));
        }

        public static void Error(string str)
        {
            if (_logger == null) return;

            _logger.Log(LogLevel.Error, str);
        }

        public static void Debug(string format, params object[] args)
        {
            Debug(string.Format(format, args));
        }

        public static void Debug(string str)
        {
            if (_logger == null) return;

            _logger.Log(LogLevel.Debug, str);
        }
    }
}
