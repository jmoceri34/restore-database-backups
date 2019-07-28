using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;


namespace RestoreDatabaseBackups
{
    internal class RestoreTool
    {
        private readonly string ConnectionString = ConfigurationManager.ConnectionStrings["Local"].ConnectionString;
        private readonly string LocalDownloadFilePath;

        internal RestoreTool()
        {
            LocalDownloadFilePath = ConfigurationManager.AppSettings["LocalDownloadFilePath"];
        }

        internal void RestoreDatabases(IList<string> databases)
        {
            foreach (string db in databases)
            {
                var fileList = GetDatabaseFileList(db);
                RestoreDatabase(db, fileList.DataName, fileList.LogName);
            }
        }

        private DatabaseFileList GetDatabaseFileList(string localDatabasePath)
        {
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string sqlQuery = @"RESTORE FILELISTONLY FROM DISK = @localDatabasePath";
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(sqlQuery, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@localDatabasePath", localDatabasePath);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        var fileList = new DatabaseFileList();
                        while (reader.Read())
                        {
                            string type = reader["Type"].ToString();
                            switch (type)
                            {
                                case "D":
                                    fileList.DataName = reader["LogicalName"].ToString();
                                    break;
                                case "L":
                                    fileList.LogName = reader["LogicalName"].ToString();
                                    break;
                            }
                        }
                        return fileList;
                    }
                }
            }
        }

        private void RestoreDatabase(string localDatabasePath, string fileListDataName, string fileListLogName)
        {
            Console.WriteLine(string.Format("Restoring database {0}...", localDatabasePath));
            string fileListDataPath = Directory.GetParent(LocalDownloadFilePath).Parent.FullName + @"\DATA\" + fileListDataName + ".mdf";
            string fileListLogPath = Directory.GetParent(LocalDownloadFilePath).Parent.FullName + @"\DATA\" + fileListLogName + ".ldf";

            string sql = @"RESTORE DATABASE @dbName FROM DISK = @path WITH recovery,
        MOVE @fileListDataName to @fileListDataPath,
        MOVE @fileListLogName to @fileListLogPath";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.CommandTimeout = 7200;
                    command.Parameters.AddWithValue("@dbName", fileListDataName);
                    command.Parameters.AddWithValue("@path", localDatabasePath);
                    command.Parameters.AddWithValue("@fileListDataName", fileListDataName);
                    command.Parameters.AddWithValue("@fileListDataPath", fileListDataPath);
                    command.Parameters.AddWithValue("@fileListLogName", fileListLogName);
                    command.Parameters.AddWithValue("@fileListLogPath", fileListLogPath);

                    command.ExecuteNonQuery();
                }
            }
            Console.WriteLine(string.Format("Database restoration complete for {0}.", localDatabasePath));
        }
    }
}
