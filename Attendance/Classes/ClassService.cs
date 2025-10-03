using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Attendance.Classes
{
    class ClassService
    {
        public static int ExtractGrade(string name) {
            // 中文年级优先
            if (name.Contains("初一")) return 1;
            if (name.Contains("初二")) return 2;
            if (name.Contains("初三")) return 3;
            if (name.Contains("高一")) return 4;
            if (name.Contains("高二")) return 5;
            if (name.Contains("高三")) return 6;

            // 英文 classX 排在中文年级之后
            var match = Regex.Match(name.ToLower(), @"class\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int classNum))
            {
                return 10 + classNum; // 英文班级排在中文之后
            }


            return 100; // 未识别的排最后
        }
        public static int ExtractClassNumber(string name)
        {
            var match = Regex.Match(name, @"[（(](\d+)[）)]");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
            {
                return num;
            }

            // 如果是 classX 格式，也提取数字
            var classMatch = Regex.Match(name.ToLower(), @"class\s*(\d+)");
            if (classMatch.Success && int.TryParse(classMatch.Groups[1].Value, out int classNum))
            {
                return classNum;
            }

            return 0;
        }
    }
}
