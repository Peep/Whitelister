using System;
using System.IO;
using System.Text;

namespace BMRFME.Whitelist
{
    public class Logger
    {
        #region LogLevel enum

        public enum LogLevel
        {
            Debug,
            Info,
            Warn,
            Error,
            Crash
        }

        #endregion

        private readonly FileInfo _logFile;
        private readonly StreamWriter _writer;
        public LogLevel Level;
        public bool LogToConsole;

        public Logger(string file)
        {
            _logFile = new FileInfo(file);
            _writer = new StreamWriter(_logFile.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true
            };
        }

        public void Close()
        {
            lock (_writer)
                _writer.Close();
        }

        public string PrettyFormat(int indentLevel = 0, params object[] args)
        {
            var builder = new StringBuilder();
            builder.AppendLine(args.ToString());
            for (int i = 0; i < args.Length; i++)
            {
                Type argType = args[i].GetType();
                if (argType.IsArray)
                    builder.Append(PrettyFormat(indentLevel + 1, args[i]));
                else
                {
                    builder.AppendFormat("{0}{1}{2}", new string('\n', indentLevel), args[i], Environment.NewLine);
                }
            }

            return builder.ToString();
        }

        public void Log(LogLevel level, string format, params object[] args)
        {
            if (level < Level)
                return;

            string logStr = string.Format("{0} : {1} : {2}",
                                          Enum.GetName(typeof(LogLevel), level),
                                          DateTime.Now.ToLongTimeString(),
                                          string.Format(format, args));
            lock (_writer)
            {
                if (LogToConsole && level >= LogLevel.Debug)
                    Console.WriteLine(logStr);

                _writer.WriteLine(logStr);
            }
        }

        public void Debug(string format, params object[] args)
        {
            Log(LogLevel.Debug, format, args);
        }

        public void Info(string format, params object[] args)
        {
            Log(LogLevel.Info, format, args);
        }

        public void Warn(string format, params object[] args)
        {
            Log(LogLevel.Warn, format, args);
        }

        public void Error(string format, params object[] args)
        {
            Log(LogLevel.Error, format, args);
        }

        public void Crash(string format, params object[] args)
        {
            Log(LogLevel.Crash, format, args);
        }
    }
}
