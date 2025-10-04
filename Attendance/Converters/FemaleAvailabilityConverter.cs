using Attendance.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Attendance.Converters
{
    public class FeMaleAvailabilityConverter : IValueConverter
    {
        // 判断女
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var students = value as ObservableCollection<Student>;
            if (students == null || students.Count == 0)
                return false;

            return students?.Any(s => s.Gender == Student.GenderEnum.female) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
