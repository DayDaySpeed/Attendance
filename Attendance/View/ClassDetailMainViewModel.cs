using Attendance.Classes;
using Attendance.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Attendance.View
{
    public class ClassDetailMainViewModel : ObservableObject
    {
        //班级数据
        private Cla classData;
        public Cla ClassData
        {
            get => classData;
            set => SetProperty(ref classData, value);
        }
        //班级学生列表
        public ObservableCollection<Student> Students { get; }
        //选中的学生
        private Student selectedStudent;
        public Student SelectedStudent
        {
            get => selectedStudent;
            set => SetProperty(ref selectedStudent, value);
        }
        //构造函数
        public ClassDetailMainViewModel(Cla cla)
        {
            classData = cla;
            Students = classData.Students;
            //默认不选中学生
            SelectedStudent = null;
        }
        //添加学生命令
        public ICommand AddStudentCommand => new RelayCommand(() =>
        {
            var vm = new StudentEditViewModel();
            var window = new StudentEditWindow(vm);
            if (window.ShowDialog() == true)
            {
                var result = window.Result;
                result.ClassId = classData.id;
                Students.Add(result);
                SelectedStudent = result;
                ClassStorageService.AddStudent(result);
            }
        });

        //删除学生命令
        public ICommand DeleteStudentCommand => new RelayCommand<Student>(student =>
        {
            Console.WriteLine($"删除学生：{student?.Name}");
            if (student != null)
            {
                Students.Remove(student);
                ClassStorageService.DeleteStudent(student);
            }
        });
        //编辑学生命令
        public ICommand EditStudentCommand => new RelayCommand<Student>(student =>
        {
            if (student == null) return;
            var vm = new StudentEditViewModel(student);
            var window = new StudentEditWindow(vm);
            if (window.ShowDialog() == true)
            {
                var updated = window.Result;
                student.Name = updated.Name;
                student.Gender = updated.Gender;
                student.StudentNumber = updated.StudentNumber;
                //更新
                ClassStorageService.UpdateStudent(student);
            }
        });

        //选择学生命令
        public ICommand SelectStudentCommand => new RelayCommand<Student>(student =>
        {
            SelectedStudent = student;
        });
    }


}
