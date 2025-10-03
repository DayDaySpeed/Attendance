using Attendance.Classes;
using Attendance.Importer;
using Attendance.Poems;
using Attendance.PoemsSearch;
using Attendance.Services;
using Attendance.Theme;
using Attendance.Utils;
using Attendance.weather;
using Microsoft.Win32;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace Attendance
{
    public class MainViewModel : ObservableObject
    {

        //主题背景
        //勾选主题
        public bool IsBlackTheme => Background is SolidColorBrush brush && brush.Color == Colors.Black;
        public bool IsWhiteTheme => Background is SolidColorBrush brush && brush.Color == Colors.White;

        //背景色
        private Brush _background = Brushes.White; // 默认系统背景色
        public Brush Background
        {
            get => _background;
            set => SetProperty(ref _background, value);
        }
        //字体色
        private Brush foreground = Brushes.Black;
        public Brush Foreground
        {
            get => foreground;
            set => SetProperty(ref foreground, value);
        }

        private bool isDarkTheme = false;
        public bool IsDarkTheme
        {
            get => isDarkTheme;
            set => SetProperty(ref isDarkTheme, value);
        
        }

        //加载搜索系统
        public SearchViewModel SearchVM { get; } = new();

        // 初始化 ClassList（避免空引用）
        public ObservableCollection<Cla> Classes { get; set; } = new ObservableCollection<Cla>();
        public ObservableCollection<Cla> SelectedClasses { get; set; } = new();

        //保存Scrollviewer的高度,让诗词系统自适应大小
        private double lastScrollViewerHeight;
        public double LastScrollViewerHeight
        {
            get => lastScrollViewerHeight;
            set => SetProperty(ref lastScrollViewerHeight, value);
        }


        //异步加载天气时间系统 
        public WeatherCardViewModel TimeVM { get; set; } = new();
        //异步加载两首诗句
        //左边一首
        public DailyPoemViewModel DailyPoemVM1 { get; } = new();
        //右边一首
        public DailyPoemViewModel DailyPoemVM2 { get; } = new();
        //异步加载每日一言诗词
        public DailyQuoteService DailyQuote { get; } = new();



        //声明导入图片命令
        public ICommand ImportBackgroundCommand { get; }
        //声明切换主题命令
        public ICommand SetThemeCommand { get; }

        public ICommand RenameClassCommand => new RelayCommand<Cla>(cla =>
        {
            Debug.WriteLine($"重命名触发：{cla?.Name}");
            if (cla != null)
                cla.IsEditing = true;
        });

        public ICommand StopEditingCommand => new RelayCommand<Cla>(cla =>
        {
            if (cla != null)
                cla.IsEditing = false;
        });




        //双击打开班级
        public ICommand ClassDoubleClickCommand => new RelayCommand<Cla>(OnClassDoubleClick);
        //选择班级
        public ICommand ClassSelectionChangedCommand => new RelayCommand<IList>(OnClassSelectionChanged);
        //声明添加班级命令
        public ICommand AddClassCommand { get; }
        //声明刷新班级命令
        public ICommand RefreshCommand { get; }
        //声明删除班级命令
        public ICommand DeleteSelectedClassesCommand => new RelayCommand(DeleteSelectedClasses);
        //声明全局刷新命令
        public ICommand RefreshAllCommand => new RelayCommand(() => {
            //刷新天气系统
            TimeVM.InitAsyncSafe();
            //刷新每日诗词系统
            DailyPoemVM1.RefreshPoemAsync();
            DailyPoemVM2.RefreshPoemAsync();
            //刷新每日一言和每日诗词
            _ = DailyQuote.RefreshAsync(); // 异步加载每日内容
        });
        //声明刷新天气命令
        public ICommand RefreshTimeWeatherCommand => new RelayCommand(() => TimeVM.InitAsyncSafe());
        //刷新每日诗词命令
        public ICommand RefreshDailyPoemCommand => new RelayCommand(() => {
            DailyPoemVM1.RefreshPoemAsync();
            DailyPoemVM2.RefreshPoemAsync();
        });
        //声明刷新每日一言诗词命令
        public ICommand RefreshDailyQuoteCommand => new RelayCommand(() => _ = DailyQuote.RefreshAsync());
        //数据库进度条
        public PoemsImportProgress ImportProgress { get; } = new();

        //初始化数据库
        private async Task InitializeDatabaseAsync()
        {
            await PoemsDatabaseInitializer.InitializeAsync(ImportProgress);
        }
        public MainViewModel()
        {
            //初始化班级非异步
            Console.WriteLine("初始化班级");
            Task.Run(() => DatabaseInitializer.Initialize());
            // 异步初始化数据库
            _ = InitializeDatabaseAsync();
            //加载用户主题
            LoadUserTheme();

            //绑定导入图片命令
            ImportBackgroundCommand = new RelayCommand(ImportBackground);

            //绑定切换主题命令
            SetThemeCommand = new RelayCommand<ThemePayload>(SetTheme);
            //绑定添加班级命令
            AddClassCommand = new RelayCommand(AddClass);
            //绑定刷新班级命令
            RefreshCommand = new RelayCommand(SortClasses);

          

            // 加载班级数据
            Classes = ClassStorageService.Load();
            

        }


        //加载主题
        private void LoadUserTheme()
        {
            var config = UserThemeStorage.Load();

            if (!string.IsNullOrEmpty(config.BackgroundImagePath) && File.Exists(config.BackgroundImagePath))
            {
                var image = new BitmapImage(new Uri(config.BackgroundImagePath, UriKind.Absolute));
                Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
            }
            else if (!string.IsNullOrEmpty(config.BackgroundColorHex))
            {
                Background = (Brush)new BrushConverter().ConvertFromString(config.BackgroundColorHex);
            }

            if (!string.IsNullOrEmpty(config.ForegroundColorHex))
            {
                Foreground = (Brush)new BrushConverter().ConvertFromString(config.ForegroundColorHex);
            }
            OnPropertyChanged(nameof(IsBlackTheme));
            OnPropertyChanged(nameof(IsWhiteTheme));

        }
        //保存主题
        private void SaveUserTheme(string? imagePath = null)
        {
            var config = new UserThemeConfig
            {
                BackgroundImagePath = imagePath,
                BackgroundColorHex = Background is SolidColorBrush bg ? bg.Color.ToString() : null,
                ForegroundColorHex = Foreground is SolidColorBrush fg ? fg.Color.ToString() : null
            };

            UserThemeStorage.Save(config);
            OnPropertyChanged(nameof(IsBlackTheme));
            OnPropertyChanged(nameof(IsWhiteTheme));
        }

        //设置主题方法
        private void SetTheme(ThemePayload theme)
        {
            if (theme.Background != null)
                Background = theme.Background;

            if (theme.Foreground != null)
                Foreground = theme.Foreground;

            SaveUserTheme(); // 保存颜色设置
        }


        //导入背景图片方法
        private void ImportBackground()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片文件 (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var image = new BitmapImage(new Uri(dialog.FileName, UriKind.Absolute));
                    Background = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
                    Foreground = Brushes.White; // 切换为白色字体以适应深色背景
                    SaveUserTheme(dialog.FileName); // 保存图片路径
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法加载图片：" + ex.Message);
                }
            }
        }

        //双击
        private void OnClassDoubleClick(Cla selectedClass)
        {
            if (selectedClass == null) return;

            var window = new ClassDetailWindow(selectedClass, Application.Current.MainWindow);
            window.Show();
        }

        //选择班级
        private void OnClassSelectionChanged(IList selectedItems)
        {
            SelectedClasses.Clear();
            foreach (Cla item in selectedItems.OfType<Cla>())
            {
                SelectedClasses.Add(item);
            }
        }

        //删除班级
        private void DeleteSelectedClasses()
        {
            if (SelectedClasses != null) { 
                var result = MessageBox.Show(
                    $"确定要删除选中的 {SelectedClasses.Count} 个班级吗？",
                    "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
                foreach (var cla in SelectedClasses.ToList())
                {
                    Classes.Remove(cla);
                }
                ClassStorageService.Save(Classes);
                SelectedClasses.Clear();
            }
        }


        //添加班级
        private void AddClass() {
            var cla = new Cla { Name = "class" };
            Classes.Add(cla);
            ClassStorageService.AddClass(cla);
            cla.IsEditing = true;
        }


        //刷新班级
        private void SortClasses()
        {
            var sorted = Classes.OrderBy(c => ClassService.ExtractGrade(c.Name))
                                .ThenBy(c => ClassService.ExtractClassNumber(c.Name))
                                .ToList();

            Classes.Clear();
            foreach (var item in sorted)
            {
                Classes.Add(item);
            }
            ClassStorageService.Save(Classes);
        }


    }
}
