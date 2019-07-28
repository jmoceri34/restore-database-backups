using System;
using System.IO;

namespace RestoreDatabaseBackups
{
    public static class Log
    {
        public static void Message(string message)
        {
            using (StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + @"\Log.txt", true))
            {
                Console.WriteLine(message);
                writer.WriteLine(message);
            }
        }
    }
}
