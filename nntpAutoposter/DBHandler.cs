using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Mono.Data.Sqlite;
using Util;

namespace nntpAutoposter
{
    class DBHandler
    {
        private static readonly ILog log = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Object lockObject = new Object();
        private static DBHandler _instance;
        public static DBHandler Instance
        {
            get
            {
                if(_instance == null)
                {
                    lock(lockObject)
                    {
                        if (_instance == null)
                            _instance = new DBHandler();
                    }
                }
                return _instance;
            }
        }

        private String _connectionString;

        private DBHandler()
        {
            DetermineConnectionString();
            InitializeDatabase();
        }

        private void DetermineConnectionString()
        {
            String codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            String path = Uri.UnescapeDataString(uri.Path);
            String assemblyDirectory = Path.GetDirectoryName(path);

            String dbFilePath = Path.Combine(assemblyDirectory, "nntpAutoPoster.Sqlite3.db");
            _connectionString = String.Format("URI=file:{0},version=3", dbFilePath);
        }

        private void InitializeDatabase()
        {
            List<DBScript> dbScripts = LoadDbScripts();
            Int64 highestScriptVersion = (Int64)Math.Floor(
                dbScripts.OrderByDescending(s => s.ScriptNumber).First().ScriptNumber);

            using (SqliteConnection conn = new SqliteConnection(_connectionString))
            {
                conn.Open();

                Int64 dbVersion;

                using(SqliteCommand versionCmd = conn.CreateCommand())
                {
                    versionCmd.CommandText = "PRAGMA user_version";
                    dbVersion = (Int64) versionCmd.ExecuteScalar();
                }

                if (dbVersion >= highestScriptVersion)
                    return;

                var scriptsToApply = dbScripts.Where(s => s.ScriptNumber >= dbVersion + 1).OrderBy(s => s.ScriptNumber);

                using (SqliteTransaction trans = conn.BeginTransaction())
                {
                    using (SqliteCommand ddlCmd = conn.CreateCommand())
                    {
                        ddlCmd.Transaction = trans;
                        foreach(var script in scriptsToApply)
                        {
                            ddlCmd.CommandText = script.DdlStatement;
                            ddlCmd.ExecuteNonQuery();
                        }
                        ddlCmd.CommandText = "PRAGMA user_version = " + highestScriptVersion;
                        ddlCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }
        }

        private List<DBScript> LoadDbScripts()
        {
            List<DBScript> scripts = new List<DBScript>();
            DirectoryInfo scriptFolder = new DirectoryInfo("dbScripts");
            if (!scriptFolder.Exists)
                return scripts;

            foreach (FileInfo scriptFile in scriptFolder.GetFileSystemInfos("*.sql"))
            {
                Decimal scriptNumber;
                if (Decimal.TryParse(scriptFile.NameWithoutExtension(), 
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out scriptNumber))
                {
                    using(StreamReader reader = scriptFile.OpenText())
                    {
                        String ddl = reader.ReadToEnd();
                        scripts.Add(new DBScript{
                            ScriptNumber = scriptNumber,
                            DdlStatement = ddl
                        });
                    }
                }
            }

            return scripts;
        }

        public UploadEntry GetNextUploadEntryToUpload()
        {
            using (SqliteConnection conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries 
                                        WHERE UploadedAt IS NULL 
                                          AND Cancelled = 0
                                        ORDER BY PriorityNum DESC, CreatedAt ASC
                                        LIMIT 1";
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        UploadEntry uploadEntry = null;
                        if (reader.Read())
                        {
                            uploadEntry = GetUploadEntryFromReader(reader);
                        }
                        return uploadEntry;
                    }
                }
            }
        }

        public List<UploadEntry> GetUploadEntriesToNotifyIndexer()
        {
            List<UploadEntry> uploadEntries = new List<UploadEntry>();
            using (SqliteConnection conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries 
                                        WHERE ObscuredName IS NOT NULL 
                                          AND NotifiedIndexerAt IS NULL
                                          AND Cancelled = 0
                                        ORDER BY CreatedAt ASC";
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uploadEntries.Add(GetUploadEntryFromReader(reader));
                        }
                    }
                }
            }
            return uploadEntries;
        }

        public List<UploadEntry> GetUploadEntriesToVerify()
        {
            List<UploadEntry> uploadEntries = new List<UploadEntry>();
            using (SqliteConnection conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries 
                                        WHERE UploadedAt IS NOT NULL
                                          AND SeenOnIndexerAt IS NULL
                                          AND Cancelled = 0
                                          AND (
                                            ObscuredName IS NULL
                                            OR 
                                            (ObscuredName IS NOT NULL AND NotifiedIndexerAt IS NOT NULL)
                                          )
                                        ORDER BY CreatedAt ASC";
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uploadEntries.Add(GetUploadEntryFromReader(reader));
                        }
                    }
                }
            }
            return uploadEntries;
        }

        public UploadEntry GetActiveUploadEntry(String name)
        {
            using (SqliteConnection conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries 
                                        WHERE Name = @name 
                                            AND Cancelled = 0";
                    cmd.Parameters.Add(new SqliteParameter("@name", name));
                    using(SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        UploadEntry uploadEntry = null;
                        if(reader.Read())
                        {
                            uploadEntry = GetUploadEntryFromReader(reader);
                            if (reader.Read())
                            {
                                throw new Exception("Got more than one result matching this name. The database is not consistent.");
                            }
                        }
                        return uploadEntry;
                    }
                }
            }
        }

        public void AddNewUploadEntry(UploadEntry uploadEntry)
        {
            using (SqliteConnection conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (SqliteTransaction trans = conn.BeginTransaction())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = @"UPDATE UploadEntries SET Cancelled = 1 WHERE Name = @name";
                        cmd.Parameters.Add(new SqliteParameter("@name", uploadEntry.Name));
                        Int32 cancelledEntries = cmd.ExecuteNonQuery();
                        if(cancelledEntries > 0)
                            log.InfoFormat("{0} upload entries were cancelled by a re-add of an existing upload.", cancelledEntries);

                        cmd.CommandText = @"INSERT INTO UploadEntries(
                                                            Name, 
                                                            Size,
                                                            CleanedName,
                                                            ObscuredName,
                                                            RemoveAfterVerify, 
                                                            CreatedAt,
                                                            UploadedAt,
                                                            NotifiedIndexerAt,
                                                            SeenOnIndexerAt,
                                                            Cancelled,
                                                            WatchFolderShortName,
                                                            UploadAttempts,
                                                            RarPassword,
                                                            PriorityNum,
                                                            NzbContents,
                                                            IsRepost)
                                                    VALUES(
                                                            @name,
                                                            @size,
                                                            @cleanedName,
                                                            @ObscuredName,
                                                            @removeAfterVerify,
                                                            @createdAt, 
                                                            @uploadedAt,
                                                            @notifiedIndexerAt,
                                                            @seenOnIndexerAt,
                                                            @cancelled,
                                                            @watchFolderShortName,
                                                            @uploadAttempts,
                                                            @rarPassword,
                                                            @priorityNum,
                                                            @nzbContents,
                                                            @isRepost)";
                        cmd.Parameters.Add(new SqliteParameter("@name", uploadEntry.Name));
                        cmd.Parameters.Add(new SqliteParameter("@size", uploadEntry.Size));
                        cmd.Parameters.Add(new SqliteParameter("@cleanedName", uploadEntry.CleanedName));
                        cmd.Parameters.Add(new SqliteParameter("@ObscuredName", uploadEntry.ObscuredName));
                        cmd.Parameters.Add(new SqliteParameter("@removeAfterVerify", uploadEntry.RemoveAfterVerify));
                        cmd.Parameters.Add(new SqliteParameter("@createdAt", GetDbValue(uploadEntry.CreatedAt)));
                        cmd.Parameters.Add(new SqliteParameter("@uploadedAt", GetDbValue(uploadEntry.UploadedAt)));
                        cmd.Parameters.Add(new SqliteParameter("@notifiedIndexerAt", GetDbValue(uploadEntry.NotifiedIndexerAt)));
                        cmd.Parameters.Add(new SqliteParameter("@seenOnIndexerAt", GetDbValue(uploadEntry.SeenOnIndexAt)));
                        cmd.Parameters.Add(new SqliteParameter("@cancelled", GetDbValue(uploadEntry.Cancelled)));
                        cmd.Parameters.Add(new SqliteParameter("@watchFolderShortName", uploadEntry.WatchFolderShortName));
                        cmd.Parameters.Add(new SqliteParameter("@uploadAttempts", uploadEntry.UploadAttempts));
                        cmd.Parameters.Add(new SqliteParameter("@rarPassword", uploadEntry.RarPassword));
                        cmd.Parameters.Add(new SqliteParameter("@priorityNum", uploadEntry.PriorityNum));
                        cmd.Parameters.Add(new SqliteParameter("@nzbContents", uploadEntry.NzbContents));
                        cmd.Parameters.Add(new SqliteParameter("@isRepost", GetDbValue(uploadEntry.IsRepost)));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "select last_insert_rowid()";
                        cmd.Parameters.Clear();
                        uploadEntry.ID = (Int64)cmd.ExecuteScalar();                        
                    }
                    trans.Commit();
                }
            }
        }

        public void UpdateUploadEntry(UploadEntry uploadEntry)
        {
            using (SqliteConnection conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE UploadEntries SET 
                                            Name = @name,
                                            CleanedName = @cleanedName,
                                            ObscuredName = @ObscuredName,
                                            RemoveAfterVerify = @removeAfterVerify,
                                            UploadedAt = @uploadedAt,
                                            NotifiedIndexerAt = @notifiedIndexerAt,
                                            SeenOnIndexerAt = @seenOnIndexerAt,
                                            Cancelled = @cancelled,
                                            WatchFolderShortName = @watchFolderShortName,
                                            UploadAttempts = @uploadAttempts,
                                            RarPassword = @rarPassword,
                                            PriorityNum = @priorityNum,
                                            NzbContents = @nzbContents,
                                            IsRepost = @isRepost
                                        WHERE ROWID = @rowId";
                    cmd.Parameters.Add(new SqliteParameter("@name", uploadEntry.Name));
                    cmd.Parameters.Add(new SqliteParameter("@cleanedName", uploadEntry.CleanedName));
                    cmd.Parameters.Add(new SqliteParameter("@ObscuredName", uploadEntry.ObscuredName));                    
                    cmd.Parameters.Add(new SqliteParameter("@removeAfterVerify", uploadEntry.RemoveAfterVerify));
                    cmd.Parameters.Add(new SqliteParameter("@uploadedAt", GetDbValue(uploadEntry.UploadedAt)));
                    cmd.Parameters.Add(new SqliteParameter("@notifiedIndexerAt", GetDbValue(uploadEntry.NotifiedIndexerAt)));
                    cmd.Parameters.Add(new SqliteParameter("@seenOnIndexerAt", GetDbValue(uploadEntry.SeenOnIndexAt)));
                    cmd.Parameters.Add(new SqliteParameter("@cancelled", GetDbValue(uploadEntry.Cancelled)));
                    cmd.Parameters.Add(new SqliteParameter("@watchFolderShortName", uploadEntry.WatchFolderShortName));
                    cmd.Parameters.Add(new SqliteParameter("@uploadAttempts", uploadEntry.UploadAttempts));
                    cmd.Parameters.Add(new SqliteParameter("@rarPassword", uploadEntry.RarPassword));
                    cmd.Parameters.Add(new SqliteParameter("@priorityNum", uploadEntry.PriorityNum));
                    cmd.Parameters.Add(new SqliteParameter("@nzbContents", uploadEntry.NzbContents));
                    cmd.Parameters.Add(new SqliteParameter("@isRepost", GetDbValue(uploadEntry.IsRepost)));
                    cmd.Parameters.Add(new SqliteParameter("@rowId", uploadEntry.ID));
                    
                    cmd.ExecuteNonQuery();                   
                }
            }
        }

        private static UploadEntry GetUploadEntryFromReader(SqliteDataReader reader)
        {
            UploadEntry uploadEntry = new UploadEntry();

            uploadEntry.ID = (Int64)reader["ROWID"];
            uploadEntry.Name = reader["Name"] as String;
            uploadEntry.Size = (Int64)reader["Size"];
            uploadEntry.CleanedName = reader["CleanedName"] as String;
            uploadEntry.ObscuredName = reader["ObscuredName"] as String;
            uploadEntry.RemoveAfterVerify = GetBoolean(reader["RemoveAfterVerify"]);
            uploadEntry.CreatedAt = GetDateTime(reader["CreatedAt"]);
            uploadEntry.UploadedAt = GetNullableDateTime(reader["UploadedAt"]);
            uploadEntry.NotifiedIndexerAt = GetNullableDateTime(reader["NotifiedIndexerAt"]);
            uploadEntry.SeenOnIndexAt = GetNullableDateTime(reader["SeenOnIndexerAt"]);
            uploadEntry.Cancelled = GetBoolean(reader["Cancelled"]);
            uploadEntry.WatchFolderShortName = reader["WatchFolderShortName"] as String;
            uploadEntry.UploadAttempts = (Int64) reader["UploadAttempts"];
            uploadEntry.RarPassword = reader["RarPassword"] as String;
            uploadEntry.PriorityNum = (Int64)reader["PriorityNum"];
            uploadEntry.NzbContents = reader["NzbContents"] as String;
            uploadEntry.IsRepost = GetBoolean(reader["IsRepost"]);

            return uploadEntry;
        }

        private static Object GetDbValue(Boolean boolean)
        {
            return boolean ? 1 : 0;
        }

        private static Object GetDbValue(Nullable<DateTime> dateTime)
        {
            if (!dateTime.HasValue)
                return DBNull.Value;
            return dateTime.Value.ToString("o");
        }

        private static Boolean GetBoolean(Object dbValue)
        {
            Int64 boolValue = (Int64)dbValue;
            return boolValue == 1;
        }

        private static DateTime GetDateTime(Object dbValue)
        {
            String dateTimeStr = dbValue as String;
            return DateTime.Parse(dateTimeStr, null, DateTimeStyles.RoundtripKind);
        }

        private static Nullable<DateTime> GetNullableDateTime(Object dbValue)
        {
            String dateTimeStr = dbValue as String;
            DateTime result;
            if (dateTimeStr != null && DateTime.TryParse(dateTimeStr, null, DateTimeStyles.RoundtripKind, out result))
                return result;

            return null;
        }
    }
}
