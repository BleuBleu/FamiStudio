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
        Error,
        Count
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

        public static string[] lastMessage = new string[(int)LogSeverity.Count];

        public static void LogMessage(LogSeverity severity, string msg)
        {
            if ((int)severity >= (int)MinSeverity && LogOutput != null)
                LogOutput.LogMessage(SeverityStrings[(int)severity] + msg);

            Debug.WriteLine(SeverityStrings[(int)severity] + msg);

            lastMessage[(int)severity] = msg;
        }

        public static void LogMessageConditional(bool condition, LogSeverity severity, string msg)
        {
            if (condition)
                LogMessage(severity, msg);
        }

        public static void SetLogOutput(ILogOutput log, LogSeverity minSeverity = LogSeverity.Info)
        {
            Debug.Assert(LogOutput == null);

            LogOutput = log;
            MinSeverity = minSeverity;
        }

        public static string GetLastMessage(LogSeverity severity)
        {
            return lastMessage[(int)severity];
        }

        public static void ClearLastMessages()
        {
            lastMessage = new string[(int)LogSeverity.Count];
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
