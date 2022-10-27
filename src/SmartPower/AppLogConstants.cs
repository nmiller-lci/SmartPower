using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;

namespace SmartPower
{
    public static class AppLogConstants
    {
#if DEBUG
        public static LogEventLevel LogDefaultEventLevel { get; } = LogEventLevel.Debug;
#else
        public static LogEventLevel LogDefaultEventLevel { get; } = LogEventLevel.Information;
#endif

        public static readonly LoggingLevelSwitch LogLevelCurrent = new LoggingLevelSwitch(LogDefaultEventLevel);
        public static readonly string LogFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        public const string LogFileNameBase = "SmartPower";
        public const string LogFileNameExtension = "log";
        public static readonly string LogFileNameFull = Path.Combine(LogFolder, $"{LogFileNameBase}.{LogFileNameExtension}");  // Note: The actual log filename will vary based on date/time stamp.
        public const long LogFileSizeLimitBytes = 1024 * 1024 * 10;
        public const int LogFileRetainedCountLimit = 7;
        public const int LogFlushIntervalMs = 1000;
        public static readonly string LogFileOutputTemplate = $"{{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}} [{{Level:u3}}:{{{Constants.SourceContextPropertyName}:l}}] {{Message:l}}{{NewLine}}{{Exception}}";
        public static readonly string LogcatConsoleOutputTemplate = $"{{Timestamp:HH:mm:ss.fff}} [{{Level}}] {{Message:l}}{{NewLine:l}}{{Exception:l}}";
        public static readonly string LogConsoleOutputTemplate = $"[{{Level}}:{{{Constants.SourceContextPropertyName}:l}}] {{Message:l}}{{NewLine:l}}{{Exception:l}}";
        public static readonly string TemperatureSensorFilePrefix = "TemperatureSensor";
        public static readonly string BatteryMonitorFilePrefix = "BatteryMonitor";
    }
}
