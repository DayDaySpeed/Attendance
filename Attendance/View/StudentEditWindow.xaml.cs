using Attendance.Classes;
using System.Windows;


namespace Attendance.View
{
    /// <summary>
    /// StudentEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StudentEditWindow : Window
    {
        public StudentEditWindow(StudentEditViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.CloseAction = result =>
            {
                DialogResult = result;
                Close();
            };
        }
        //返回编辑后的学生对象,根据确定或取消按钮的点击情况
        public Student Result => ((StudentEditViewModel)DataContext).ToStudent();

    }

}
