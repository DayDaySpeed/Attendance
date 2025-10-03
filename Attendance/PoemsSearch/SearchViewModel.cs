using Attendance.Poems;
using Attendance.Utils;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace Attendance.PoemsSearch
{
    public class SearchViewModel : ObservableObject
    {
        private readonly SearchService _service = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db", "poems.db"));

        public ObservableCollection<SearchItem> Results { get; } = new();

        public string Keyword { get => _keyword; set => SetProperty(ref _keyword, value); }
        public string SelectedMode { get => _selectedMode; set => SetProperty(ref _selectedMode, value); }
        public string DetailContent { get => _detailContent; set => SetProperty(ref _detailContent, value); }

        private string _keyword;
        private string _selectedMode = "Poem";
        private string _detailContent;



        public Visibility SearchVisibility { get => _searchVisibility; set => SetProperty(ref _searchVisibility, value); }
        public Visibility DetailVisibility { get => _detailVisibility; set => SetProperty(ref _detailVisibility, value); }
        public Visibility WaitVisibility
        {
            get => _waitVisibility;
            set => SetProperty(ref _waitVisibility, value);
        }
        public Visibility IsVisibility
        {
            get => _IsVisibility;
            set => SetProperty(ref _IsVisibility, value);
        }
        //初始不显示搜索框
        private Visibility _IsVisibility = Visibility.Collapsed;
        //初始显示等待
        private Visibility _waitVisibility = Visibility.Visible;
        private Visibility _searchVisibility = Visibility.Visible;
        private Visibility _detailVisibility = Visibility.Collapsed;
        

        public SearchViewModel()
        {
            // 初始化时检查数据库是否准备好
            _ = CheckDatabaseReadyAsync();
        }

        public async Task CheckDatabaseReadyAsync()
        {
            var ready = await DatabaseReadyNotifier.ReadySignal.Task;
            if (ready)
            {
                WaitVisibility = Visibility.Collapsed;
                IsVisibility = Visibility.Visible;
            }
        }


        public async Task SearchAsync()
        {
            Results.Clear();
            DetailVisibility = Visibility.Collapsed;
            SearchVisibility = Visibility.Visible;

            var items = await _service.SearchAsync(Keyword, SelectedMode);
            foreach (var item in items)
                Results.Add(item);
        }

        public async Task ShowDetailAsync(SearchItem item)
        {
            DetailContent = await _service.LoadDetailAsync(item);
            SearchVisibility = Visibility.Collapsed;
            DetailVisibility = Visibility.Visible;
        }

        public void BackToSearch()
        {
            SearchVisibility = Visibility.Visible;
            DetailVisibility = Visibility.Collapsed;
        }
    }
}
