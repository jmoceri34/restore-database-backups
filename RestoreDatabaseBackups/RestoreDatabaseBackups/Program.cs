using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;


namespace RestoreDatabaseBackups
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var localDownloadFilePath = ConfigurationManager.AppSettings["LocalDownloadFilePath"];
                if (args.Length > 0)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] == "-D" || args[i] == "-d")
                        {
                            new DownloadTool().DownloadProductionBackups();
                        }
                        else if (args[i] == "-R" || args[i] == "-r")
                        {
                            var info = new List<string>();
                            if (i != args.Length - 1 && (args[i + 1] != "-D" && args[i + 1] != "-d"))
                            {
                                string actualName = args[i + 1];
                                info.Add(actualName);
                            }
                            else
                            {
                                var dirFiles = Directory.GetFiles(localDownloadFilePath);
                                foreach (string file in dirFiles)
                                {
                                    info.Add(Path.GetFileName(file));
                                }
                            }
                            new RestoreTool().RestoreDatabases(info);
                        }
                    }
                }
                else
                {
                    var result = new DownloadTool().DownloadProductionBackups();
                    new RestoreTool().RestoreDatabases(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
