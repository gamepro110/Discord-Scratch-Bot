using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScratchBot
{
    internal class FileLogger
    {
        private static readonly string m_logPath = $"{Environment.CurrentDirectory}/_Bot_Log.LOG";

        internal static void LogToFile(string _log, bool _append = true)
        {
            if (!File.Exists(m_logPath))
            {
                File.Create(m_logPath);
            }

            List<string> _lines = new List<string>(10);
            if (_append)
            {
                _lines = File.ReadAllLines(m_logPath).ToList();
            }
            _lines.Add(string.Format("[{0} - {1}]", DateTime.Now.ToString("yyyy-MM-dd--HH-mm"), _log));
            File.WriteAllText(m_logPath, _lines.Aggregate("", (current, s) => current + (s + Environment.NewLine)));
        }
    }
}