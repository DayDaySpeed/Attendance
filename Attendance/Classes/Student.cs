using Attendance.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.Classes
{
    public class Student : ObservableObject
    {
        //id（由数据库给出）
        public int id { get; set; }


        //学号
        private long studentNumber;
        public long StudentNumber
        {
            get => studentNumber;
            set => SetProperty(ref studentNumber, value);
        }

        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }




        private GenderEnum? gender;
        public GenderEnum? Gender
        {
            get { return gender; }
            set => SetProperty(ref gender,value);
        }

 
        public enum GenderEnum
        { 
            male, female
        }

        // 新增属性：用于数据库关联
        public long ClassId { get; set; }

        public string TooltipText => $"ID: {StudentNumber}, 性别: {Gender}";
    }
}
