using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Wallet_Payment;

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

                // 新增Web时间追踪表
                string webTrackingSql = @"CREATE TABLE IF NOT EXISTS WebTimeTracking (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Url TEXT,
                                Title TEXT,
                                Domain TEXT,
                                StartTime TEXT,
                                EndTime TEXT,
                                Duration TEXT,
                                Date TEXT
                            );";
                new SQLiteCommand(webTrackingSql, conn).ExecuteNonQuery();


                // 初始化5个模式
                string[] modes = { "阅读", "工作", "电影", "游戏", "自定义" };
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

        public static void AddWebTimeTracking(TimeTrackingData data)
        {
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string sql = @"INSERT INTO WebTimeTracking (Url, Title, Domain, StartTime, EndTime, Duration, Date)
                               VALUES (@url, @title, @domain, @startTime, @endTime, @duration, @date);";
                var cmd = new SQLiteCommand(sql, conn);
                cmd.Parameters.AddWithValue("@url", data.Url ?? "");
                cmd.Parameters.AddWithValue("@title", data.Title ?? "");
                cmd.Parameters.AddWithValue("@domain", data.Domain ?? "");
                cmd.Parameters.AddWithValue("@startTime", data.StartTime ?? "");
                cmd.Parameters.AddWithValue("@endTime", data.EndTime ?? "");
                cmd.Parameters.AddWithValue("@duration", data.Duration ?? "");
                cmd.Parameters.AddWithValue("@date", DateTime.Today.ToString("yyyy-MM-dd"));
                cmd.ExecuteNonQuery();
            }
        }

        public static List<TimeTrackingData> GetTodayWebTracking()
        {
            var list = new List<TimeTrackingData>();
            try
            {
                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    string sql = @"SELECT Url, Title, Domain, StartTime, EndTime, Duration
                                   FROM WebTimeTracking
                                   WHERE Date = @d
                                   ORDER BY Id DESC;";
                    var cmd = new SQLiteCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@d", DateTime.Today.ToString("yyyy-MM-dd"));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new TimeTrackingData
                            {
                                Url = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                Title = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Domain = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                StartTime = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                EndTime = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Duration = reader.IsDBNull(5) ? "" : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 获取Web时间追踪数据时出错: {ex.Message}");
            }
            return list;
        }

        public static void AddOrUpdateWebTimeTracking(TimeTrackingData data)
        {
            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                string selectSql = "SELECT Id, Duration FROM WebTimeTracking WHERE Domain=@domain AND Date=@date";
                var cmd = new SQLiteCommand(selectSql, conn);
                cmd.Parameters.AddWithValue("@domain", data.Domain ?? "");
                cmd.Parameters.AddWithValue("@date", DateTime.Today.ToString("yyyy-MM-dd"));
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    long oldDuration = long.Parse(reader.GetString(1));
                    reader.Close();
                    string updateSql = "UPDATE WebTimeTracking SET Duration=@duration, EndTime=@endTime WHERE Id=@id";
                    var updateCmd = new SQLiteCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@duration", (oldDuration + long.Parse(data.Duration)).ToString());
                    updateCmd.Parameters.AddWithValue("@endTime", data.EndTime ?? "");
                    updateCmd.Parameters.AddWithValue("@id", id);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    reader.Close();
                    AddWebTimeTracking(data);
                }
            }
        }
    }
}