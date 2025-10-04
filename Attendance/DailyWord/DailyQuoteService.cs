using Attendance.Poems;
using Attendance.Utils;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;


namespace Attendance.DailyWord
{
    public class DailyQuoteService : ObservableObject
    {
        /// <summary>
        /// 每日一言（来自 API）
        /// </summary>
        private string _quoteText = "正在加载每日一言...";
        public string QuoteText
        {
            get => _quoteText;
            set => SetProperty(ref _quoteText, value);
        }

        /// <summary>
        /// 每日诗句（来自数据库）
        /// </summary>
        private string _sentenceText = "正在加载每日诗句...";
        public string SentenceText
        {
            get => _sentenceText;
            set => SetProperty(ref _sentenceText, value);
        }

        public DailyQuoteService()
        {
            LoadAsync();
        }

        /// <summary>
        /// 异步加载每日内容
        /// </summary>
        public async Task LoadAsync()
        {
            //先并行获取每日一言和等待数据库准备好
            var quoteTask = GetDailyQuoteAsync();
            QuoteText = await quoteTask;
            await DatabaseReadyNotifier.ReadySignal.Task;//先等待数据库准备好
            //数据库加载好了以后再获取每日诗句
            var sentenceTask = GetRandomSentenceAsync();
            SentenceText = await sentenceTask;
        }

        //刷新
        public async Task RefreshAsync()
        {
            await LoadAsync();
        }


        /// <summary>
        /// 从 API 获取每日一言
        /// </summary>
        private async Task<string> GetDailyQuoteAsync()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync("https://v1.hitokoto.cn/");
                using var doc = JsonDocument.Parse(response);
                var text = doc.RootElement.GetProperty("hitokoto").GetString();
                var from = doc.RootElement.GetProperty("from").GetString();
                var fromWho = doc.RootElement.TryGetProperty("from_who", out var who) ? who.GetString() : null;

                string author = string.IsNullOrWhiteSpace(fromWho) ? "" : $" · {fromWho}";
                Debug.WriteLine($"{text}\n—— {from}{author}");
                return $"{text}\n—— {from}{author}";
            }
            catch (Exception ex)
            {
                return $"无法获取每日一言：{ex.Message}";
            }
        }


        /// <summary>
        /// 从 SQLite 数据库随机获取一句诗句
        /// </summary>
        private async Task<string> GetRandomSentenceAsync()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", "poems.db");

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                await conn.OpenAsync();

                using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM Sentences", conn);
                int count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                if (count == 0) return "数据库中暂无诗句。";

                int offset = new Random().Next(count);
                using var cmd = new SqliteCommand("SELECT Name, FromSource FROM Sentences LIMIT 1 OFFSET @offset", conn);
                cmd.Parameters.AddWithValue("@offset", offset);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    string name = reader.GetString(0);
                    string from = reader.GetString(1);
                    return $"{name}\n——《{from}》";
                }

                return "未能读取诗句。";
            }
            catch (Exception ex)
            {
                return $"读取诗句失败：{ex.Message}";
            }
        }
    }
}
