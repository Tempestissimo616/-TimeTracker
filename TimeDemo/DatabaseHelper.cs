using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace TimeDemo
{
    public static class DatabaseHelper
    {
        private static string dbPath = "SoftwareUsage.db";
        private static string connStr = $"Data Source={dbPath};Version=3;";

        public static void Init()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = @"CREATE TABLE IF NOT EXISTS SoftwareUsage (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ProcessName TEXT,
                                Date TEXT,
                                Duration INTEGER
                            );";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();
            }
        }

        public static void AddUsage(string processName, DateTime date, int duration)
        {
            if (duration <= 0) return;
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = @"INSERT INTO SoftwareUsage (ProcessName, Date, Duration)
                               VALUES (@p, @d, @t);";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p", processName);
                cmd.Parameters.AddWithValue("@d", date.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@t", duration);
                cmd.ExecuteNonQuery();
            }
        }

        public static List<(string ProcessName, int Duration)> GetTodayUsage()
        {
            var list = new List<(string, int)>();
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = @"SELECT ProcessName, SUM(Duration) as TotalDuration
                               FROM SoftwareUsage
                               WHERE Date = @d
                               GROUP BY ProcessName
                               ORDER BY TotalDuration DESC;";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@d", DateTime.Today.ToString("yyyy-MM-dd"));
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add((reader.GetString(0), reader.GetInt32(1)));
                    }
                }
            }
            return list;
        }
    }
}