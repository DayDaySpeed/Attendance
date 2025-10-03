using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Collections.ObjectModel;
using Attendance.Classes;

namespace Attendance.Converters
{
    public class GenderAvailabilityConverter : IValueConverter
    {
        // value 是 DisplayedStudents 集合
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var students = value as ObservableCollection<Student>;
            if (students == null || students.Count == 0)
                return false;

            return students.Any(s => s.Gender != null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
