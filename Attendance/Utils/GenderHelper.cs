using Attendance.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.Utils
{
    public static class GenderHelper
    {
        public static readonly Dictionary<string, Student.GenderEnum> GenderMap = new()
    {
        { "男", Student.GenderEnum.male },
        { "女", Student.GenderEnum.female }
    };

        public static string ToDisplay(Student.GenderEnum? gender) =>
            gender switch
            {
                Student.GenderEnum.male => "男",
                Student.GenderEnum.female => "女",
                _ => ""
            };

        public static Student.GenderEnum? FromDisplay(string display)
        {
            if (display != null && GenderMap.TryGetValue(display, out var value))
                return value;
            return null;
        }

    }

}
