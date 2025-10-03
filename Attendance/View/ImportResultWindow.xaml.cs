using System;

using System.IO;

using System.Windows;


namespace Attendance
{
    /// <summary>
    /// ImportResultWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ImportResultWindow : Window
    {
        private readonly string folderPath;

        public ImportResultWindow(int successCount, int skippedCount, DateTime endTime, TimeSpan duration, string logFolderPath)
        {
            InitializeComponent();

            SuccessText.Text = $"✅ 成功导入学生数: {successCount}";
            SkippedText.Text = $"⚠️ 跳过无效行数: {skippedCount}";
            EndTimeText.Text = $"📤 导入结束时间: {endTime:G}";
            DurationText.Text = $"⏱ 总耗时: {duration.TotalSeconds:F2} 秒";

            folderPath = logFolderPath;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(folderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
            }
            else
            {
                MessageBox.Show("日志文件夹不存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}
