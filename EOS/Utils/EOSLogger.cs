using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GTFO.API.Attributes.ConfigModel;
using GTFO.API.Utilities;
using System.ComponentModel;

namespace EOS.Utils
{
    public static class EOSLogger
    {
        private static readonly ManualLogSource _logger = Logger.CreateLogSource("EOS");

        #pragma warning disable CS8618
        [Section("Debug")]
        [ConfigEntry("Enable verbose logging", false)]
        [Description("Toggles additional debug logging of information to the console")]
        public static ConfigEntry<bool> VerboseEnabled { get; private set; }
        #pragma warning restore CS8618

        internal static void Setup()
        {
            ConfigFile cfg = new(Path.Combine(Paths.ConfigPath, "EOS.cfg"), true);
            cfg.ConfigReloaded += (o, args) => Warning("Config file changed: " + cfg.ConfigFilePath);
            ConfigBinder.BindToType(cfg, typeof(EOSLogger));
            SafeFileSystemWatcher.Create(cfg);
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

        public static void Warning(string format, params object[] args)
        {
            Warning(string.Format(format, args));
        }

        public static void Warning(string str)
        {
            if (_logger == null) return;

            _logger.Log(LogLevel.Warning, str);
        }

        public static void Message(string format, params object[] args)
        {
            Message(string.Format(format, args));
        }

        public static void Message(string str)
        {
            if (_logger == null) return;

            _logger.Log(LogLevel.Message, str);
        }

        public static void Info(string format, params object[] args)
        {
            Info(string.Format(format, args));
        }

        public static void Info(string str)
        {
            if (_logger == null) return;

            _logger.Log(LogLevel.Info, str);
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

        public static void Verbose(string str, LogLevel level = LogLevel.Debug)
        {
            if (VerboseEnabled.Value)
            {
                switch (level)
                {
                    case LogLevel.Error:
                        Error(str);
                        return;
                    case LogLevel.Warning:
                        Warning(str);
                        return;
                    case LogLevel.Message:
                        Message(str);
                        return;
                    case LogLevel.Info:
                        Info(str);
                        return;
                    case LogLevel.Debug:
                        Debug(str);
                        return;
                }
            }
        }
    }
}
