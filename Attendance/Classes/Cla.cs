using Attendance.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Attendance.Classes
{
    public class Cla : ObservableObject
    {

        //数据库ID
        public int id; 

        //班级名字
        private string name;
        public string Name {
            get => name;
            set => SetProperty(ref name, value);
        }
        private bool isEditing;
        public bool IsEditing
        {
            get => isEditing;
            set { isEditing = value; OnPropertyChanged(); }
        }
        public ICommand StopEditingCommand => new RelayCommand(() => IsEditing = false);
        public ObservableCollection<Student> Students { get; set; } = new ObservableCollection<Student>();
        // = new Student() {Name="zhang", Id=1, Gender= Student.GenderEnum.female }
    }
}
