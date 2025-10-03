using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.PoemsSearch
{
    public class SearchService
    {
        private readonly string _dbPath;

        public SearchService(string dbPath)
        {
            _dbPath = dbPath;
        }

        public async Task<List<SearchItem>> SearchAsync(string keyword, string mode)
        {
            var results = new List<SearchItem>();
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();

            string sql = mode switch
            {
                "Poem" => "SELECT Id, Title, Writer, Content FROM Poems WHERE Title LIKE @kw OR Content LIKE @kw LIMIT 50",
                "Sentence" => "SELECT Id, Name, FromSource FROM Sentences WHERE Name LIKE @kw OR FromSource LIKE @kw LIMIT 50",
                "Writer" => "SELECT Id, Name, SimpleIntro FROM Writers WHERE Name LIKE @kw OR SimpleIntro LIKE @kw LIMIT 50",
                _ => throw new ArgumentException("无效搜索模式")
            };

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string display = mode switch
                {
                    "Poem" => $"{reader.GetString(1)}\n{reader.GetString(2)}\n{reader.GetString(3).Substring(0, Math.Min(50, reader.GetString(3).Length))}",
                    "Sentence" => $"{reader.GetString(2)}\n{reader.GetString(1).Substring(0, Math.Min(50, reader.GetString(1).Length))}",
                    "Writer" => $"{reader.GetString(1)}\n{reader.GetString(2).Substring(0, Math.Min(50, reader.GetString(2).Length))}",
                    _ => ""
                };

                results.Add(new SearchItem
                {
                    Id = reader.GetInt32(0),
                    DisplayText = display,
                    Type = mode
                });
            }

            return results;
        }

        public async Task<string> LoadDetailAsync(SearchItem item)
        {
            using var conn = new SqliteConnection($"Data Source={_dbPath}");
            await conn.OpenAsync();

            string sql = item.Type switch
            {
                "Poem" => "SELECT Title, Writer, Content FROM Poems WHERE Id = @id",
                "Sentence" => "SELECT FromSource, Name FROM Sentences WHERE Id = @id",
                "Writer" => "SELECT Name, SimpleIntro FROM Writers WHERE Id = @id",
                _ => throw new ArgumentException("无效类型")
            };

            using var cmd = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", item.Id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return item.Type switch
                {
                    "Poem" => $"{reader.GetString(0)}\n{reader.GetString(1)}\n{reader.GetString(2)}",
                    "Sentence" => $"{reader.GetString(0)}\n{reader.GetString(1)}",
                    "Writer" => $"{reader.GetString(0)}\n{reader.GetString(1)}",
                    _ => ""
                };
            }

            return "未找到内容";
        }
    }

}
