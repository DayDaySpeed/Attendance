using Attendance.Classes;
using Attendance.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Attendance
{
    /// <summary>
    /// ClassDetailWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ClassDetailWindow : Window
    {
        public Cla ClassData { get; }

        public ClassDetailWindow(Cla cla, Window ownerWindow)
        {
            InitializeComponent();
            var viewModel = new ClassDetailMainViewModel(cla);
            DataContext = viewModel;


            // 设置窗口所有者
            this.Owner = ownerWindow;

            // 设置窗口略小于主窗口
            double scale = 0.85;
            this.Width = ownerWindow.Width * scale;
            this.Height = ownerWindow.Height * scale;

            // 设置窗口居中于主窗口
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = ownerWindow.Left + (ownerWindow.Width - this.Width) / 2;
            this.Top = ownerWindow.Top + (ownerWindow.Height - this.Height) / 2;
        }
    }

}
