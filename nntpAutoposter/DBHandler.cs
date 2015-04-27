using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Data.Sqlite;

namespace nntpAutoposter
{
    class DBHandler
    {
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

        private DBHandler()
        {
            InitializeDataBase();
        }

        private SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConfigurationManager.ConnectionStrings["DBConnection"].ConnectionString);
        }

        private void InitializeDataBase()
        {
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteTransaction trans = conn.BeginTransaction())
                {
                    using (SqliteCommand ddlCmd = conn.CreateCommand())
                    {
                        ddlCmd.Transaction = trans;
                        ddlCmd.CommandText = @"CREATE TABLE IF NOT EXISTS 
                                           UploadEntries(
                                            Name TEXT, 
                                            Size INTEGER,
                                            CleanedName TEXT, 
                                            HashedName TEXT, 
                                            RemoveAfterVerify INTEGER,
                                            CreatedAt TEXT,
                                            UploadedAt TEXT,
                                            NotifiedIndexerAt TEXT,
                                            SeenOnIndexerAt TEXT,
                                            Cancelled INTEGER)";
                        ddlCmd.ExecuteNonQuery();
                        ddlCmd.CommandText = "CREATE INDEX IF NOT EXISTS UploadEntries_Name_idx ON UploadEntries (Name)";
                        ddlCmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
            }
        }

        public UploadEntry GetNextUploadEntryToUpload()
        {
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries 
                                        WHERE UploadedAt IS NULL 
                                          AND Cancelled = 0
                                        ORDER BY CreatedAt ASC
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
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries 
                                        WHERE HashedName IS NOT NULL 
                                          AND NotifiedIndexerAt IS NULL
                                          AND Cancelled = 0
                                        ORDER BY CreatedAt ASC";
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        UploadEntry uploadEntry = null;
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
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries 
                                        WHERE UploadedAt IS NOT NULL
                                          AND SeenOnIndexerAt IS NULL
                                          AND Cancelled = 0
                                          AND (
                                            HashedName IS NULL
                                            OR 
                                            (HashedName IS NOT NULL AND NotifiedIndexerAt IS NOT NULL)
                                          )
                                        ORDER BY CreatedAt ASC";
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        UploadEntry uploadEntry = null;
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
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ROWID, * from UploadEntries WHERE Name = @name AND Cancelled = 0";
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

        public void AddNewUploadEntry(UploadEntry uploadentry)
        {
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteTransaction trans = conn.BeginTransaction())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = @"UPDATE UploadEntries SET Cancelled = 1 WHERE Name = @name";
                        cmd.Parameters.Add(new SqliteParameter("@name", uploadentry.Name));
                        cmd.ExecuteNonQuery(); //TODO: log here how many other entries were cancelled.

                        cmd.CommandText = @"INSERT INTO UploadEntries(
                                                            Name, 
                                                            Size,
                                                            CleanedName,
                                                            HashedName,
                                                            RemoveAfterVerify, 
                                                            CreatedAt,
                                                            UploadedAt,
                                                            NotifiedIndexerAt,
                                                            SeenOnIndexerAt,
                                                            Cancelled)
                                                    VALUES(
                                                            @name,
                                                            @size,
                                                            @cleanedName,
                                                            @hashedName,
                                                            @removeAfterVerify,
                                                            @createdAt, 
                                                            @uploadedAt,
                                                            @notifiedIndexerAt,
                                                            @seenOnIndexerAt,
                                                            @cancelled)";
                        cmd.Parameters.Add(new SqliteParameter("@name", uploadentry.Name));
                        cmd.Parameters.Add(new SqliteParameter("@size", uploadentry.Size));
                        cmd.Parameters.Add(new SqliteParameter("@cleanedName", uploadentry.CleanedName));
                        cmd.Parameters.Add(new SqliteParameter("@hashedName", uploadentry.HashedName));
                        cmd.Parameters.Add(new SqliteParameter("@removeAfterVerify", uploadentry.RemoveAfterVerify));
                        cmd.Parameters.Add(new SqliteParameter("@createdAt", GetDbValue(uploadentry.CreatedAt)));
                        cmd.Parameters.Add(new SqliteParameter("@uploadedAt", GetDbValue(uploadentry.UploadedAt)));
                        cmd.Parameters.Add(new SqliteParameter("@notifiedIndexerAt", GetDbValue(uploadentry.NotifiedIndexerAt)));
                        cmd.Parameters.Add(new SqliteParameter("@seenOnIndexerAt", GetDbValue(uploadentry.SeenOnIndexAt)));
                        cmd.Parameters.Add(new SqliteParameter("@cancelled", GetDbValue(uploadentry.Cancelled)));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "select last_insert_rowid()";
                        cmd.Parameters.Clear();
                        uploadentry.ID = (Int64)cmd.ExecuteScalar();                        
                    }
                    trans.Commit();
                }
            }
        }

        public void UpdateUploadEntry(UploadEntry uploadEntry)
        {
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE UploadEntries SET 
                                            Name = @name,
                                            CleanedName = @cleanedName,
                                            HashedName = @hashedName,
                                            RemoveAfterVerify = @removeAfterVerify,
                                            UploadedAt = @uploadedAt,
                                            NotifiedIndexerAt = @notifiedIndexerAt,
                                            SeenOnIndexerAt = @seenOnIndexerAt,
                                            Cancelled = @cancelled
                                        WHERE ROWID = @rowId";
                    cmd.Parameters.Add(new SqliteParameter("@name", uploadEntry.Name));
                    cmd.Parameters.Add(new SqliteParameter("@cleanedName", uploadEntry.CleanedName));
                    cmd.Parameters.Add(new SqliteParameter("@hashedName", uploadEntry.HashedName));                    
                    cmd.Parameters.Add(new SqliteParameter("@removeAfterVerify", uploadEntry.RemoveAfterVerify));
                    cmd.Parameters.Add(new SqliteParameter("@uploadedAt", GetDbValue(uploadEntry.UploadedAt)));
                    cmd.Parameters.Add(new SqliteParameter("@notifiedIndexerAt", GetDbValue(uploadEntry.NotifiedIndexerAt)));
                    cmd.Parameters.Add(new SqliteParameter("@seenOnIndexerAt", GetDbValue(uploadEntry.SeenOnIndexAt)));
                    cmd.Parameters.Add(new SqliteParameter("@cancelled", GetDbValue(uploadEntry.Cancelled)));
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
            uploadEntry.HashedName = reader["HashedName"] as String;
            uploadEntry.RemoveAfterVerify = GetBoolean(reader["RemoveAfterVerify"]);
            uploadEntry.CreatedAt = GetDateTime(reader["CreatedAt"]);
            uploadEntry.UploadedAt = GetNullableDateTime(reader["UploadedAt"]);
            uploadEntry.NotifiedIndexerAt = GetNullableDateTime(reader["NotifiedIndexerAt"]);
            uploadEntry.SeenOnIndexAt = GetNullableDateTime(reader["SeenOnIndexerAt"]);
            uploadEntry.Cancelled = GetBoolean(reader["Cancelled"]);

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
