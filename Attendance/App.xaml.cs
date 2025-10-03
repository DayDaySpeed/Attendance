using Attendance.Poems;
using Attendance.Classes;
using System;

using System.Threading.Tasks;
using System.Windows;

namespace Attendance
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            SQLitePCL.Batteries.Init(); // ✅ 初始化 SQLite


            // 手动创建并显示主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

        }
    }
}
