using Attendance.Classes;
using Attendance.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace Attendance.View
{
    public class StudentEditViewModel : ObservableObject
    {
        //学生ID
        private long studentNumber;
        public long StudentNumber
        {
            get => studentNumber;
            set => SetProperty(ref studentNumber, value);
        }
        //学生姓名
        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }
        //学生性别
        private string gender;
        public string Gender
        {
            get => gender;
            set => SetProperty(ref gender, value);
        }
        //关闭窗口的回调
        public Action<bool>? CloseAction { get; set; }

        public ICommand ConfirmCommand { get; } 

        public ICommand CancelCommand { get; }



        //构造方法
        public StudentEditViewModel(Student student = null)
        {
            //确定和取消命令
            ConfirmCommand = new RelayCommand(() => CloseAction?.Invoke(true));
            CancelCommand = new RelayCommand(() => CloseAction?.Invoke(false));
            if (student != null)
            {
                StudentNumber = student.StudentNumber;
                Name = student.Name;
                Gender = GenderHelper.ToDisplay(student.Gender);
            }
        }
        //返回学生对象
        public Student ToStudent()
        {
            return new Student
            {
                StudentNumber = this.StudentNumber,
                Name = this.Name,
                Gender = GenderHelper.FromDisplay(this.Gender)
            };
        }

    }

}
