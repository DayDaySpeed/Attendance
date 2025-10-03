using Attendance.Animation;
using Attendance.Classes;
using Attendance.Utils;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Attendance.View
{
    public class RollCallViewModel : ObservableObject
    {
        // 所有班级列表
        public ObservableCollection<Cla> Classes { get; set; }
       

        // 所选人数
        public int SelectedCount { get => selectedCount; set => SetProperty(ref selectedCount, value); }
        private int selectedCount = 1;

        // 性别偏好（全部、男、女）
        public string SelectedGenderPreference { get => selectedGenderPreference; set => SetProperty(ref selectedGenderPreference, value); }
        private string selectedGenderPreference = "全部";

        // 学号末尾数字偏好（0–9，-1 表示无偏好）
        public int SelectedTailDigit { get => selectedTailDigit; set => SetProperty(ref selectedTailDigit, value); }
        private int selectedTailDigit = -1;
     
        // 当前选中的班级
        private Cla selectedClass;
        public Cla SelectedClass
        {
            get => selectedClass;
            set
            {
                SetProperty(ref selectedClass, value);
                if (selectedClass != null)
                {
                    DisplayedStudents = selectedClass.Students;
                    // ✅ 重置设置项
                    SelectedGenderPreference = "全部";
                    SelectedTailDigit = -1;
                }
            }
        }
        // 抽取结果
        public ObservableCollection<Student> SelectedStudents { get; set; } = new();

        // 控制横向 ScrollViewer 的宽度
        public double StudentPanelWidth
        {
            get => studentPanelWidth;
            set { studentPanelWidth = value; OnPropertyChanged(); }
        }
        private double studentPanelWidth = SystemParameters.PrimaryScreenWidth * 0.66;
        // 当前展示的学生列表（用于初始布局）
        private ObservableCollection<Student> displayedStudents = new();
        public ObservableCollection<Student> DisplayedStudents
        {
            get => displayedStudents;
            set => SetProperty(ref displayedStudents, value);
        }

        // 单抽
        private ObservableCollection<Student> scrollingStudents = new();
        public ObservableCollection<Student> ScrollingStudents
        {
            get => scrollingStudents;
            set => SetProperty(ref scrollingStudents, value);
        }
        //双抽
        private ObservableCollection<Student> leftScrollingStudents = new();
        public ObservableCollection<Student> LeftScrollingStudents
        {
            get => leftScrollingStudents;
            set => SetProperty(ref leftScrollingStudents, value);
        }
        private ObservableCollection<Student> rightScrollingStudents = new();
        public ObservableCollection<Student> RightScrollingStudents
        {
            get => rightScrollingStudents;
            set => SetProperty(ref rightScrollingStudents, value);
        }
        //三抽
        private ObservableCollection<Student> topLeftStudents = new();
        public ObservableCollection<Student> TopLeftStudents
        {
            get => topLeftStudents;
            set => SetProperty(ref topLeftStudents, value);
        }
        private ObservableCollection<Student> topRightStudents = new();
        public ObservableCollection<Student> TopRightStudents
        {
            get => topRightStudents;
            set => SetProperty(ref topRightStudents, value);
        }
        private ObservableCollection<Student> bottomStudents = new();
        public ObservableCollection<Student> BottomStudents
        { 
            get => bottomStudents;
            set => SetProperty(ref bottomStudents, value);
        }
        // 构造函数：初始化班级列表
        public RollCallViewModel(ObservableCollection<Cla> classes)
        {
            Classes = classes;
            if (Classes.Any())
                SelectedClass = Classes.First();
        }


    }
}

