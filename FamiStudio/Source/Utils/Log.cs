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
        void ReportProgress(float progress);
        bool AbortOperation { get; }
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
        public static ILogOutput LogOutput;
        public static LogSeverity MinSeverity = LogSeverity.Info;

        public static void LogMessage(LogSeverity severity, string msg)
        {
            if ((int)severity >= (int)MinSeverity && LogOutput != null)
                LogOutput.LogMessage(SeverityStrings[(int)severity] + msg);
            Debug.WriteLine(SeverityStrings[(int)severity] + msg);
        }

        public static void SetLogOutput(ILogOutput log, LogSeverity minSeverity = LogSeverity.Info)
        {
            Debug.Assert(LogOutput == null);

            LogOutput = log;
            MinSeverity = minSeverity;
        }

        public static void ClearLogOutput()
        {
            LogOutput = null;
        }

        public static void ReportProgress(float progress)
        {
            if (LogOutput != null)
            {
                LogOutput.ReportProgress(progress);
            }
        }

        public static bool ShouldAbortOperation 
        {
            get
            {
                return LogOutput != null && LogOutput.AbortOperation;
            }
        }
    };
}
