using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Attendance.Poems
{
    public static class PoemsDatabaseInitializer
    {
        // 设置 JSON 反序列化选项，忽略大小写
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// 初始化数据库：导入诗文、作者、名句，并生成最终数据库文件
        /// </summary>
        public static async Task InitializeAsync(PoemsImportProgress progress)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string resourceDir = Path.Combine(baseDir, "Resources", "chinese-gushiwen");
            string dbDir = Path.Combine(baseDir, "db");
            string dbPath = Path.Combine(dbDir, "poems.db");

            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

            // 如果数据库已初始化，则跳过导入
            if (File.Exists(dbPath) && IsAlreadyInitialized(dbPath))
            {
                progress.Visibility = Visibility.Collapsed;
                DatabaseReadyNotifier.ReadySignal.TrySetResult(true);
                return;
            }
            //重新导入
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                await conn.OpenAsync();

                using var transaction = conn.BeginTransaction();
                try
                {
                    CreateTables(conn, transaction);
                    await ImportPoemsAsync(Path.Combine(resourceDir, "guwen"), conn, transaction, progress);
                    await ImportWritersAsync(Path.Combine(resourceDir, "writer"), conn, transaction, progress);
                    await ImportSentencesAsync(Path.Combine(resourceDir, "sentence"), conn, transaction, progress);

                    // 写入初始化标志
                    var markCmd = new SqliteCommand("INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('IsInitialized', 'true')", conn, transaction);
                    markCmd.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ 数据导入异常：{ex.Message}");
                    transaction.Rollback();
                    throw;
                }

                progress.Status = "✅ 所有数据导入成功，数据库已生成！";
                progress.Visibility = Visibility.Collapsed;
                DatabaseReadyNotifier.ReadySignal.TrySetResult(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ 导入失败：{ex.Message}");
                progress.Status = $"❌ 导入失败: {ex.Message}";
                progress.Visibility = Visibility.Collapsed;
            }
        }
        private static bool IsAlreadyInitialized(string dbPath)
        {
            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();

                var cmd = new SqliteCommand("SELECT Value FROM Meta WHERE Key = 'IsInitialized'", conn);
                var result = cmd.ExecuteScalar();

                return result?.ToString() == "true";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Meta检查失败] {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// 创建数据库表结构
        /// </summary>
        private static void CreateTables(SqliteConnection conn, SqliteTransaction transaction)
        {
            string sql = @"
                CREATE TABLE Poems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT, Dynasty TEXT, Writer TEXT, Content TEXT,
                    Type TEXT, Remark TEXT, Shangxi TEXT, Translation TEXT
                );
                CREATE TABLE Writers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT, SimpleIntro TEXT, DetailIntro TEXT
                );
                CREATE TABLE Sentences (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT, FromSource TEXT
                );
                CREATE TABLE IF NOT EXISTS Meta (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
                );";

            using var cmd = new SqliteCommand(sql, conn, transaction);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 导入诗文数据
        /// </summary>
        private static async Task ImportPoemsAsync(string folderPath, SqliteConnection conn, SqliteTransaction transaction, PoemsImportProgress progress)
        {
            if (!Directory.Exists(folderPath)) return;

            var files = Directory.GetFiles(folderPath, "*.json");
            int totalLines = files.Sum(file => File.ReadAllLines(file).Length);

            progress.Total = totalLines;
            progress.Current = 0;
            progress.Status = "📚 正在导入诗文...";

            foreach (var file in files)
            {
                foreach (var line in File.ReadLines(file))
                {
                    Poem? poem = null;
                    try
                    {
                        poem = JsonSerializer.Deserialize<Poem>(line, JsonOptions);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ 反序列化失败：{ex.Message}");
                        Debug.WriteLine($"⚠️ 错误内容：{line}");
                        continue;
                    }

                    if (poem == null) continue;

                    try
                    {
                        using var cmd = new SqliteCommand(@"
                            INSERT INTO Poems (Title, Dynasty, Writer, Content, Type, Remark, Shangxi, Translation)
                            VALUES (@Title, @Dynasty, @Writer, @Content, @Type, @Remark, @Shangxi, @Translation)", conn, transaction);

                        cmd.Parameters.AddWithValue("@Title", poem.Title ?? "");
                        cmd.Parameters.AddWithValue("@Dynasty", poem.Dynasty ?? "");
                        cmd.Parameters.AddWithValue("@Writer", poem.Writer ?? "");
                        cmd.Parameters.AddWithValue("@Content", poem.Content ?? "");
                        cmd.Parameters.AddWithValue("@Type", poem.Type != null ? string.Join(",", poem.Type) : "");
                        cmd.Parameters.AddWithValue("@Remark", poem.Remark ?? "");
                        cmd.Parameters.AddWithValue("@Shangxi", poem.Shangxi ?? "");
                        cmd.Parameters.AddWithValue("@Translation", poem.Translation ?? "");
                        cmd.ExecuteNonQuery();

                        progress.Current++;
                        progress.Status = $"✅ 导入：{poem.Title} - {poem.Writer}";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ 写入数据库失败：{ex.Message}");
                    }

                    await Task.Delay(1);
                }
            }
        }

        /// <summary>
        /// 导入作者数据
        /// </summary>
        private static async Task ImportWritersAsync(string folderPath, SqliteConnection conn, SqliteTransaction transaction, PoemsImportProgress progress)
        {
            if (!Directory.Exists(folderPath)) return;

            var files = Directory.GetFiles(folderPath, "*.json");
            int totalLines = files.Sum(file => File.ReadAllLines(file).Length);

            progress.Total = totalLines;
            progress.Current = 0;
            progress.Status = "🧑‍🎓 正在导入作者...";

            foreach (var file in files)
            {
                foreach (var line in File.ReadLines(file))
                {
                    Writer? writer = null;
                    try
                    {
                        writer = JsonSerializer.Deserialize<Writer>(line, JsonOptions);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ 反序列化失败：{ex.Message}");
                        Debug.WriteLine($"⚠️ 错误内容：{line}");
                        continue;
                    }

                    if (writer == null) continue;

                    try
                    {
                        using var cmd = new SqliteCommand(@"
                            INSERT INTO Writers (Name, SimpleIntro, DetailIntro)
                            VALUES (@Name, @SimpleIntro, @DetailIntro)", conn, transaction);

                        cmd.Parameters.AddWithValue("@Name", writer.Name ?? "");
                        cmd.Parameters.AddWithValue("@SimpleIntro", writer.SimpleIntro ?? "");
                        cmd.Parameters.AddWithValue("@DetailIntro", writer.DetailIntro ?? "");
                        cmd.ExecuteNonQuery();

                        progress.Current++;
                        progress.Status = $"✅ 导入作者：{writer.Name}";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ 写入数据库失败：{ex.Message}");
                    }

                    await Task.Delay(1);
                }
            }
        }
        /// <summary>
        /// 导入名句数据
        /// </summary>
        private static async Task ImportSentencesAsync(string folderPath, SqliteConnection conn, SqliteTransaction transaction, PoemsImportProgress progress)
        {
            if (!Directory.Exists(folderPath)) return;

            var files = Directory.GetFiles(folderPath, "*.json");
            int totalLines = files.Sum(file => File.ReadAllLines(file).Length);

            progress.Total = totalLines;
            progress.Current = 0;
            progress.Status = "📜 正在导入名句...";

            foreach (var file in files)
            {
                foreach (var line in File.ReadLines(file))
                {
                    Sentence? sentence = null;
                    try
                    {
                        sentence = JsonSerializer.Deserialize<Sentence>(line, JsonOptions);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ 反序列化失败：{ex.Message}");
                        Debug.WriteLine($"⚠️ 错误内容：{line}");
                        continue;
                    }

                    if (sentence == null) continue;

                    try
                    {
                        using var cmd = new SqliteCommand(@"
                            INSERT INTO Sentences (Name, FromSource)
                            VALUES (@Name, @FromSource)", conn, transaction);

                        cmd.Parameters.AddWithValue("@Name", sentence.Name ?? "");
                        cmd.Parameters.AddWithValue("@FromSource", sentence.From ?? "");
                        cmd.ExecuteNonQuery();

                        progress.Current++;
                        progress.Status = $"✅ 导入名句：{sentence.Name}";
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ 写入数据库失败：{ex.Message}");
                    }

                    await Task.Delay(1);
                }
            }
        }
    }
}
