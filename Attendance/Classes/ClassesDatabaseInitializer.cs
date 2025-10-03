using Microsoft.Data.Sqlite;
using System;
using System.IO;

    namespace Attendance.Classes
    {
        public static class DatabaseInitializer
        {
            private static readonly string DbFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db");
            public static readonly string DbPath = Path.Combine(DbFolder, "classes.db");

            public static void Initialize()
            {
                Directory.CreateDirectory(DbFolder);

                bool isNew = !File.Exists(DbPath);

                using var conn = new SqliteConnection($"Data Source={DbPath}");
                conn.Open();

                if (isNew)
                {
                    CreateTables(conn);
                }
                else
                {
                    EnsureColumnsExist(conn);
                }
            }

            private static void CreateTables(SqliteConnection conn)
            {
                string createTables = @"
                    CREATE TABLE Classes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT
                    );
                    CREATE TABLE Students (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        StudentNumber INTEGER,
                        Gender TEXT,
                        ClassId INTEGER,
                        FOREIGN KEY(ClassId) REFERENCES Classes(Id)
                    );";
                using var cmd = new SqliteCommand(createTables, conn);
                cmd.ExecuteNonQuery();
            }

            private static void EnsureColumnsExist(SqliteConnection conn)
            {
                var columns = new[] { "StudentNumber", "Gender", "ClassId" };
                foreach (var column in columns)
                {
                    if (!ColumnExists(conn, "Students", column))
                    {
                        using var alterCmd = new SqliteCommand($"ALTER TABLE Students ADD COLUMN {column} TEXT;", conn);
                        alterCmd.ExecuteNonQuery();
                    }
                }
            }

            private static bool ColumnExists(SqliteConnection conn, string tableName, string columnName)
            {
                using var cmd = new SqliteCommand($"PRAGMA table_info({tableName});", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(1).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
        }
    }
