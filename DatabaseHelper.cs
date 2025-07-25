using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Social_Blade_Dashboard
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
                // 原有表
                string sql = @"CREATE TABLE IF NOT EXISTS SoftwareUsage (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProcessName TEXT,
                        Date TEXT,
                        Duration INTEGER
                    );";
                new SQLiteCommand(sql, conn).ExecuteNonQuery();

                // 新增专注统计表
                string focusSql = @"CREATE TABLE IF NOT EXISTS FocusStats (
                                Mode TEXT PRIMARY KEY,
                                Minutes INTEGER
                            );";
                new SQLiteCommand(focusSql, conn).ExecuteNonQuery();

                // 初始化5个模式
                string[] modes = { "阅读", "工作", "电影", "游戏", "其他" };
                foreach (var mode in modes)
                {
                    string checkSql = "SELECT COUNT(*) FROM FocusStats WHERE Mode=@m";
                    var checkCmd = new SQLiteCommand(checkSql, conn);
                    checkCmd.Parameters.AddWithValue("@m", mode);
                    long count = (long)checkCmd.ExecuteScalar();
                    if (count == 0)
                    {
                        string insertSql = "INSERT INTO FocusStats (Mode, Minutes) VALUES (@m, 0)";
                        var insertCmd = new SQLiteCommand(insertSql, conn);
                        insertCmd.Parameters.AddWithValue("@m", mode);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public static Dictionary<string, int> GetAllFocusStats()
        {
            var dict = new Dictionary<string, int>();
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT Mode, Minutes FROM FocusStats";
                var cmd = new SQLiteCommand(sql, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dict[reader.GetString(0)] = reader.GetInt32(1);
                    }
                }
            }
            return dict;
        }

        public static void AddFocusMinutes(string mode, int minutes)
        {
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = "UPDATE FocusStats SET Minutes = Minutes + @min WHERE Mode = @m";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@min", minutes);
                cmd.Parameters.AddWithValue("@m", mode);
                cmd.ExecuteNonQuery();
            }
        }

        public static void AddUsage(string processName, DateTime date, int duration)
        {
            if (duration <= 0) return;
            System.Diagnostics.Debug.WriteLine($"AddUsage: {processName}, {date}, {duration}");
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