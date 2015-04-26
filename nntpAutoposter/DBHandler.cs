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
                                            CleanedName TEXT, 
                                            HashedName TEXT, 
                                            RemoveAfterVerify INTEGER,
                                            CreatedAt TEXT,
                                            UploadedAt TEXT,
                                            SentToIndexerAt TEXT,
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

        public UploadEntry GetActiveUploadEntry(String name)
        {
            using (SqliteConnection conn = GetConnection())
            {
                conn.Open();
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT * from UploadEntries Where Name = @name AND Cancelled = 0";
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

        public UploadEntry AddNewUploadEntry(UploadEntry newUploadEntry)
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
                        cmd.Parameters.Add(new SqliteParameter("@name", newUploadeEntry.Name));
                        cmd.ExecuteNonQuery(); //TODO: log here how many other entries were cancelled.

                        cmd.CommandText = @"INSERT INTO UploadEntries(
                                                            Name, 
                                                            CleanedName,
                                                            HashedName,
                                                            RemoveAfterVerify, 
                                                            CreatedAt,
                                                            UploadedAt,
                                                            SentToIndexerAt,
                                                            SeenOnIndexerAt
                                                            Cancelled)
                                                    VALUES(
                                                            @name,
                                                            @cleanedName,
                                                            @hashedName,
                                                            @removeAfterVerify,
                                                            @createdAt, 
                                                            @uploadedAt,
                                                            @sentToIndexerAt
                                                            @seenOnIndexerAt
                                                            @cancelled)";
                        cmd.Parameters.Add(new SqliteParameter("@name", uploadEntry.Name));
                        cmd.Parameters.Add(new SqliteParameter("@cleanedName", uploadEntry.CleanedName));
                        cmd.Parameters.Add(new SqliteParameter("@hashedName", uploadEntry.HashedName));
                        cmd.Parameters.Add(new SqliteParameter("@removeAfterVerify", uploadEntry.RemoveAfterVerify));
                        cmd.Parameters.Add(new SqliteParameter("@createdAt", GetDbValue(newUploadeEntry.CreatedAt)));
                        cmd.Parameters.Add(new SqliteParameter("@uploadedAt", GetDbValue(uploadEntry.UploadedAt)));
                        cmd.Parameters.Add(new SqliteParameter("@sentToIndexerAt", GetDbValue(uploadEntry.SentToIndexAt)));
                        cmd.Parameters.Add(new SqliteParameter("@seenOnIndexerAt", GetDbValue(uploadEntry.SeenOnIndexAt)));
                        cmd.Parameters.Add(new SqliteParameter("@cancelled", GetDbValue(uploadEntry.Cancelled)));
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "select last_insert_rowid()";
                        cmd.Parameters.Clear();
                        newUploadeEntry.ID = (Int64)cmd.ExecuteScalar();                        
                    }
                    trans.Commit();
                }
            }

            return newUploadeEntry;
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
                                            SentToIndexerAt = @sentToIndexerAt,
                                            SeenOnIndexerAt = @seenOnIndexerAt,
                                            Cancelled = @cancelled
                                        WHERE ROWID = @rowId";
                    cmd.Parameters.Add(new SqliteParameter("@name", uploadEntry.Name));
                    cmd.Parameters.Add(new SqliteParameter("@cleanedName", uploadEntry.CleanedName));
                    cmd.Parameters.Add(new SqliteParameter("@hashedName", uploadEntry.HashedName));                    
                    cmd.Parameters.Add(new SqliteParameter("@removeAfterVerify", uploadEntry.RemoveAfterVerify));
                    cmd.Parameters.Add(new SqliteParameter("@uploadedAt", GetDbValue(uploadEntry.UploadedAt)));
                    cmd.Parameters.Add(new SqliteParameter("@sentToIndexerAt", GetDbValue(uploadEntry.SentToIndexAt)));
                    cmd.Parameters.Add(new SqliteParameter("@seenOnIndexerAt", GetDbValue(uploadEntry.SeenOnIndexAt)));
                    cmd.Parameters.Add(new SqliteParameter("@cancelled", GetDbValue(uploadEntry.Cancelled)));
                    
                    cmd.ExecuteNonQuery();                   
                }
            }
        }

        private static UploadEntry GetUploadEntryFromReader(SqliteDataReader reader)
        {
            UploadEntry uploadEntry = new UploadEntry();

            uploadEntry.ID = (Int64)reader["ROWID"];
            uploadEntry.Name = reader["Name"] as String;
            uploadEntry.CleanedName = reader["CleanedName"] as String;
            uploadEntry.HashedName = reader["HashedName"] as String;
            uploadEntry.RemoveAfterVerify = GetBoolean(reader["RemoveAfterVerify"]);
            uploadEntry.CreatedAt = GetDateTime(reader["CreatedAt"]);
            uploadEntry.UploadedAt = GetNullableDateTime(reader["UploadedAt"]);
            uploadEntry.SentToIndexAt = GetNullableDateTime(reader["SentToIndexAt"]);
            uploadEntry.SeenOnIndexAt = GetNullableDateTime(reader["SeenOnIndexAt"]);
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
            Int32 boolValue = (Int32)dbValue;
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
