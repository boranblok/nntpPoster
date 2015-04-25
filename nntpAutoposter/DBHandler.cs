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

        private static UploadEntry GetUploadEntryFromReader(SqliteDataReader reader)
        {
            UploadEntry uploadEntry = new UploadEntry();

            uploadEntry.ID = (Int64)reader["ROWID"];
            uploadEntry.Name = reader["Name"] as String;
            uploadEntry.CleanedName = reader["CleanedName"] as String;
            uploadEntry.HashedName = reader["HashedName"] as String;
            uploadEntry.RemoveAfterVerify = GetBoolean(reader["RemoveAfterVerify"]);
            uploadEntry.UploadedAt = GetDateTime(reader["UploadedAt"]);
            uploadEntry.SentToIndexAt = GetDateTime(reader["SentToIndexAt"]);
            uploadEntry.SeenOnIndexAt = GetDateTime(reader["SeenOnIndexAt"]);
            uploadEntry.Cancelled = GetBoolean(reader["Cancelled"]);

            return uploadEntry;
        }

        private static Boolean GetBoolean(Object dbValue)
        {
            Int32 boolValue = (Int32)dbValue;
            return boolValue == 1;
        }

        private static Nullable<DateTime> GetDateTime(Object dbValue)
        {
            String dateTimeStr = dbValue as String;
            DateTime result;
            if (dateTimeStr != null && DateTime.TryParse(dateTimeStr, null, DateTimeStyles.RoundtripKind, out result))
                return result;

            return null;
        }
    }
}
