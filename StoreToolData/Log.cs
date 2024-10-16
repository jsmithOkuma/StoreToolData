using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreToolData
{
    class Log
    {
        string logFilePath = "log.txt";

        public Log()
        {
        }

        public void AddLogMessage(string message)
        {
            using (StreamWriter sw = File.AppendText(logFilePath))
            {
                sw.WriteLine($"{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt")} - {message}");
            }
        }
    }
}
