using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Common
{
    public class LogHandler
    {
        #region private members and constructors
        private string _path = null;
        private string _logFile = null;
        private LogHandler()
        {
            SetPathToLogFile();
            CreateLogFile();
            Console.WriteLine("Writing logs to log file: " + _logFile);
        }

        #endregion

        #region singleton members
        private static LogHandler _logHandler = null;
        public static LogHandler GetLogHandler
        {
            get
            {
                if (_logHandler == null)
                {
                    _logHandler = new LogHandler();
                }
                return _logHandler;
            }
            set { }
        }
        #endregion

        #region public methods
        public void Log(string message,
            [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            string className = CutClassName(filePath);
            string fullMessage = GetTimestamp(DateTime.Now) + ": " + className + ":" + sourceLineNumber + " (" + memberName + ") - " + message;
            Console.WriteLine(fullMessage);
            //using (StreamWriter streamWriter = new StreamWriter(_logFile, append: true))
            //{
            //    streamWriter.WriteLine(fullMessage);
            //    streamWriter.Close();
            //}

        }
        #endregion

        #region private methods
        private void SetPathToLogFile()
        {
            _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
        }
        private void CreateLogFile()
        {
            _logFile = Path.Combine(_path, "Log.txt");
            if (!File.Exists(_logFile))
            {
                File.Create(_logFile);
            }
        }
        private static string CutClassName(string filePath)
        {
            int index = filePath.LastIndexOf("\\") + 1;
            string className = filePath.Substring(index);
            return className;
        }
        private string GetTimestamp(DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm:ss:ffff");
        }
        #endregion
    }
}