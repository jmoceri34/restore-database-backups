using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RestoreDatabaseBackups
{
    /// <summary>
    /// First verify the local download file path exists, then create an Old folder in that directory if one doesn't exist,
    /// and move all files in the local download file path to that directory. Then download all specified files asynchronously.
    /// </summary>
    internal class DownloadTool
    {
        private readonly string LocalDownloadFilePath;
        private readonly string FtpAddress;
        private readonly string FtpUserName;
        private readonly string FtpPassword;

        internal DownloadTool()
        {
            FtpAddress = ConfigurationManager.AppSettings["FtpAddress"];
            FtpUserName = ConfigurationManager.AppSettings["FtpUserName"];
            FtpPassword = ConfigurationManager.AppSettings["FtpPassword"];
            LocalDownloadFilePath = ConfigurationManager.AppSettings["LocalDownloadFilePath"];
            if (!Directory.Exists(LocalDownloadFilePath))
            {
                string message = string.Format("Directory not found for {0}.", LocalDownloadFilePath);
                throw new DirectoryNotFoundException(message);
            }
            CheckOldDirectory();
            MoveFilesToOldDirectory();
        }

        private void CheckOldDirectory()
        {
            string dir = LocalDownloadFilePath + @"Old";
            if (!Directory.Exists(dir))
            {
                Console.WriteLine(string.Format("Creating directory {0}...", dir));
                Directory.CreateDirectory(dir);
            }
        }

        private void MoveFilesToOldDirectory()
        {
            var files = Directory.GetFiles(LocalDownloadFilePath);
            string destinationPath = LocalDownloadFilePath + @"Old\";
            foreach (string file in files)
            {
                Console.WriteLine(string.Format("Moving {0} to old folder...", file));
                string fileName = Path.GetFileName(file);
                File.Move(file, destinationPath + fileName);
            }
        }

        internal IList<string> DownloadProductionBackups()
        {
            IList<string> ftpFilePaths = GetFileList();

            var tasks = new List<Task>();
            foreach (string fileName in ftpFilePaths)
            {
                tasks.Add(GetTask(fileName));
                Log.Message(string.Format("Downloading {0} to {1}...", fileName, LocalDownloadFilePath));
            }

            var watch = new Stopwatch();
            watch.Start();
            Task.WaitAll(tasks.ToArray());
            watch.Stop();
            Log.Message("Concurrent Time: " + watch.Elapsed.ToString());
            Log.Message(string.Format("Completed download to {0} for all files.", LocalDownloadFilePath));

            return ftpFilePaths;
        }

        private Task GetTask(string fileName)
        {
            using (WebClient client = new WebClient())
            {
                client.Credentials = new NetworkCredential(FtpUserName, FtpPassword);
                client.DownloadFileCompleted += (sender, args) => DownloadFileCompleted(sender, args, fileName, LocalDownloadFilePath);
                return client.DownloadFileTaskAsync(new Uri(FtpAddress + fileName), LocalDownloadFilePath + fileName);
            }
        }

        private IList<string> GetFileList()
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(FtpAddress);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(FtpUserName, FtpPassword);
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day - 1;

            List<string> databaseNames = new List<string>();
            var dbNames = ConfigurationManager.AppSettings["DatabaseNames"].Split(new[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
            databaseNames.AddRange(dbNames.Select(s => s.Trim()));

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        List<string> directories = new List<string>();
                        string line = reader.ReadLine();
                        while (!reader.EndOfStream)
                        {
                            foreach (var name in databaseNames)
                            {
                                if (line.Contains(name))
                                {
                                    directories.Add(line);
                                }
                            }
                            line = reader.ReadLine();
                        }
                        return directories;
                    }
                }
            }
        }

        private void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e, string name, string path)
        {
            string message = string.Format("Download Complete for {0} to {1}.", name, path);
            Console.WriteLine(e.Error != null ? e.Error.Message : message);
        }
    }
}
