using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DrawCardGame
{
    public static class DatabaseHelper
    {
        private static readonly string dbPath = "user_cards.db";

        public static void InitializeDatabase()
        {
            if (!File.Exists(dbPath))
            {
                File.Create(dbPath).Close(); // 创建空文件
            }

            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS OwnedCards (
                        CardId INTEGER PRIMARY KEY
                    );";
                command.ExecuteNonQuery();
            }
        }

        public static void AddCardIfNotExists(int cardId)
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT OR IGNORE INTO OwnedCards (CardId)
                    VALUES ($cardId);";
                command.Parameters.AddWithValue("$cardId", cardId);
                command.ExecuteNonQuery();
            }
        }

        public static void ClearAllOwnedCards()
        {
            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM OwnedCards;";
                command.ExecuteNonQuery();
            }
        }
    }
}

