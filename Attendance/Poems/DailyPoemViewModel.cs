using Attendance.Utils;
using Microsoft.Data.Sqlite;

using System.Collections.ObjectModel;

using System.IO;


namespace Attendance.Poems
{
    public class DailyPoemViewModel : ObservableObject
    {
        private ObservableCollection<List<string>> _poemLines = new();
        public ObservableCollection<List<string>> PoemLines
        {
            get => _poemLines;
            set => SetProperty(ref _poemLines, value);
        }

        private Poem _currentPoem;
        public Poem CurrentPoem
        {
            get => _currentPoem;
            set
            {
                SetProperty(ref _currentPoem, value);
            }
        }

        public DailyPoemViewModel()
        {
            PoemLines = new ObservableCollection<List<string>>
                    {
                        new List<string> { "正", "在", "等", "待" },
                        new List<string> { "数", "据", "库", "加", "载" }
                    };

            _ = WaitForDatabaseThenLoadPoemAsync(); // 异步加载，不阻塞 UI
        }

        /// <summary>
        /// 根据 ScrollViewer 高度自动分列显示诗文内容
        /// </summary>
        public void AdaptPoemToHeight(double scrollViewerHeight)
        {
            if (CurrentPoem == null || scrollViewerHeight <= 0) return;

            int charHeight = 41;
            int maxCharsPerColumn = Math.Max(1, (int)(scrollViewerHeight / charHeight));

            string fullText = $"{CurrentPoem.Title}\n{CurrentPoem.Dynasty}·{CurrentPoem.Writer}\n{CurrentPoem.Content}";
            var allChars = fullText
                .Replace("\r", "")
                .Replace("\n", "")
                .ToCharArray()
                .Select(c => c.ToString())
                .ToList();

            var grouped = allChars
                .Select((c, i) => new { Char = c, Index = i })
                .GroupBy(x => x.Index / maxCharsPerColumn)
                .Select(g => g.Select(x => x.Char).ToList());

            PoemLines = new ObservableCollection<List<string>>(grouped);
        }

        /// <summary>
        /// 外部调用刷新诗文
        /// </summary>
        public async Task RefreshPoemAsync()
        {
            await LoadPoemAsync();
        }

        /// <summary>
        /// 等待数据库准备好后加载诗文
        /// </summary>
        public async Task WaitForDatabaseThenLoadPoemAsync()
        {
            await DatabaseReadyNotifier.ReadySignal.Task;
            await LoadPoemAsync();
        }

        /// <summary>
        /// 异步加载一首随机古诗
        /// </summary>
        private async Task LoadPoemAsync()
        {
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", "poems.db");

            using var conn = new SqliteConnection($"Data Source={dbPath}");
            await conn.OpenAsync();

            using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM Poems", conn);
            int count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            if (count == 0) return;

            int offset = new Random().Next(count);
            using var cmd = new SqliteCommand(@"
                SELECT Title, Dynasty, Writer, Content 
                FROM Poems 
                LIMIT 1 OFFSET @offset", conn);
            cmd.Parameters.AddWithValue("@offset", offset);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                CurrentPoem = new Poem
                {
                    Title = reader.GetString(0),
                    Dynasty = reader.GetString(1),
                    Writer = reader.GetString(2),
                    Content = reader.GetString(3)
                };

                PoemLines.Clear(); // 清空旧数据
                // ✅ 立即适配高度，确保显示内容
                if (App.Current.MainWindow?.DataContext is MainViewModel vm)
                {
                    AdaptPoemToHeight(vm.LastScrollViewerHeight);
                }
            }
        }
    }
}
