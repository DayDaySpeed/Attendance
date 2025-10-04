using Attendance.Classes;
using Attendance.Importer;
using Attendance.PoemsSearch;
using Attendance.View;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;






namespace Attendance
{
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resources/images/icon.ico"));
            DataContext = new MainViewModel(); // 绑定到 ViewModel

        }



        //导入
        private async void Button_import(object sender, RoutedEventArgs e)
        {

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel 文件 (*.xls;*.xlsx)|*.xls;*.xlsx|xls文件 (*.xls)|*.xls|xlsx文件 (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                //创建班级
                Cla newclass = new Cla { Name = "新班级" };
                // ✅ 写入数据库
                ClassStorageService.AddClass(newclass);
                try
                {
                    ProgressOverlay.Visibility = Visibility.Visible;
                    ImportProgressBar.Value = 0;
                    ProgressText.Text = "正在导入...";

                    // 启动翻书动画
                    var flip = (Storyboard)this.Resources["FlipBookAnimation"];
                    flip.Begin();

                    //进度条
                    var progress = new Progress<int>(percent =>
                    {
                        ImportProgressBar.Value = percent;

                        double progressBarWidth = ImportProgressBar.ActualWidth;
                        double characterWidth = BookCharacter.ActualWidth;
                        // 计算角色位置（让角色在进度条上移动）
                        double maxX = progressBarWidth - characterWidth;
                        double offsetX = (percent / 100.0) * maxX;
                        CharacterTransform.X = offsetX;

                        ProgressText.Text = $"导入进度: {percent}%";
                    });
                    //开始导入时间
                    DateTime startTime = DateTime.Now;

                    // 异步导入，避免阻塞 UI 线程
                    var importLog = await Task.Run(() =>
                        StudentImportManager.ImportStudents(newclass, dialog.FileName, progress));
                    //导入结束时间
                    DateTime endTime = DateTime.Now;
                    //导入用时
                    TimeSpan duration = endTime - startTime;

                    ProgressOverlay.Visibility = Visibility.Collapsed;
                    
                    //显示到ui
                    var vm = DataContext as MainViewModel;
                    if (vm != null && newclass != null)
                    {
                        vm.Classes.Add(newclass);
                        ////保存
                        //ClassStorageService.AddClass(newclass);
                    }

                    //导入结果
                    int successCount = StudentImportManager.ExtractCount(importLog, "成功导入学生数");
                    int skippedCount = StudentImportManager.ExtractCount(importLog, "跳过无效行数");
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string logDirectory = System.IO.Path.Combine(documentsPath, "Attendlog");
                    //显示导入结果窗口
                    var summaryWindow = new ImportResultWindow(successCount, skippedCount, endTime, duration, logDirectory);
                    summaryWindow.ShowDialog();
                    //触发重命名
                    newclass.IsEditing = true;
                }
                catch (Exception ex)
                {
                    ClassStorageService.DeleteClass(newclass);
                    ProgressOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void UpdateCharacterPosition(double progressValue)
        {
            double progressBarWidth = ImportProgressBar.ActualWidth;
            double characterWidth = BookCharacter.ActualWidth;

            // 计算角色位置（让角色在进度条上移动）
            double maxX = progressBarWidth - characterWidth;
            double offsetX = (progressValue / 100.0) * maxX;

            CharacterTransform.X = offsetX;
        }

       

        //点击点名按钮
        private void RollCallButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前 ViewModel
            var viewModel = this.DataContext as MainViewModel;
            if (viewModel != null)
            {
                var rollCallWindow = new RollCallSettingsWindow(viewModel.Classes);
                rollCallWindow.Show(); // 或 Show()，根据你的需求
            }
        }

        //搜索诗词
        private async void OnSearchClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                await vm.SearchVM.SearchAsync();
        }

        private async void OnResultClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is SearchItem item && DataContext is MainViewModel vm)
                await vm.SearchVM.ShowDetailAsync(item);
        }

        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SearchVM.BackToSearch();
        }

    }

}
