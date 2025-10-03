using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Collections.ObjectModel;
using Attendance.Classes;

namespace Attendance.Converters
{
    public class TailDigitAvailabilityConverter : IMultiValueConverter
    {
        // values[0] = 当前 ComboBoxItem 的值（0–9）
        // values[1] = DisplayedStudents 集合
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is int digit) || !(values[1] is ObservableCollection<Student> students))
                return true;

            // 如果所有学号都是 0，则禁用所有选项
            if (students.All(s => s.StudentNumber == 0))
                return false;

            // 如果至少有一个学生的尾号匹配当前选项，则启用
            return students.Any(s => s.StudentNumber % 10 == digit);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
