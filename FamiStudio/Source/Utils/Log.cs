using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public enum LogSeverity
    {
        Info,
        Warning,
        Error
    };

    public interface ILogOutput
    {
        void Log(string msg);
        LogSeverity MinSeverity { get; }
    }

    public class ScopedLogOutput : IDisposable
    {
        public ScopedLogOutput(ILogOutput log)
        {
            Log.LogOutput = log;
        }

        public void Dispose()
        {
            Log.LogOutput = null;
        }
    }

    public static class Log
    {
        private static readonly string[] SeverityStrings = new []
        {
            "Info: ",
            "Warning: ",
            "Error: "
        };

        public static ILogOutput LogOutput { get; set; }

        public static void LogMessage(LogSeverity severity, string msg)
        {
            if (LogOutput != null && (int)severity >= (int)LogOutput.MinSeverity)
            {
                LogOutput.Log(SeverityStrings[(int)severity] + msg);
            }
        }
    };
}
