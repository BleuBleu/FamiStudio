using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        void LogMessage(string msg);
    }

    public class ScopedLogOutput : IDisposable
    {
        // HACK: Working around a super strange mono compiling bug where I cant pass 
        // the interface. It wants me to add a reference to ATK sharp. Somewhat similar 
        // to this unsolved bug: https://xamarin.github.io/bugzilla-archives/30/30631/bug.html
        public ScopedLogOutput(/*ILogOutput*/ object log, LogSeverity minSeverity = LogSeverity.Info)
        {
            Log.LogOutput = log;
            Log.MinSeverity = minSeverity;
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

        // HACK: See comment above. Mono compiler bug.
        public static /*ILogOutput*/ object LogOutput;
        public static LogSeverity MinSeverity = LogSeverity.Info;

        public static void LogMessage(LogSeverity severity, string msg)
        {
            if ((int)severity >= (int)MinSeverity && (LogOutput as ILogOutput) != null)
            {
                (LogOutput as ILogOutput).LogMessage(SeverityStrings[(int)severity] + msg);
                Debug.WriteLine(SeverityStrings[(int)severity] + msg);
            }
        }
    };
}
