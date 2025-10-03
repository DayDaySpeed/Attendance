using Attendance.Classes;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Attendance.Converters
{
    public class GenderToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Student.GenderEnum gender)
            {
                if (gender == Student.GenderEnum.male)
                {
                    // 男生：蓝紫电竞风（左下 → 右上）
                    return new LinearGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(70, 130, 180), 0.0),   // SteelBlue
                    new GradientStop(Color.FromRgb(106, 79, 191), 1.0)    // 紫色
                },
                        StartPoint = new Point(0, 1),
                        EndPoint = new Point(1, 0)
                    };
                }
                else
                {
                    // 女生：粉紫电竞风（左下 → 右上）
                    return new LinearGradientBrush
                    {
                        GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(255, 182, 193), 0.0),  // LightPink
                    new GradientStop(Color.FromRgb(191, 64, 191), 1.0)    // 紫红色
                },
                        StartPoint = new Point(0, 1),
                        EndPoint = new Point(1, 0)
                    };
                }
            }

            // 默认：紫绿电竞风（Hope卡片风格）
            // 默认：深紫绿电竞风（Hope卡片风格）
            return new LinearGradientBrush
            {
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(71, 47, 139), 0.0),  // 深紫
                    new GradientStop(Color.FromRgb(48, 173, 166), 1.0)  // 深青绿
                },
                StartPoint = new Point(0, 1),
                EndPoint = new Point(1, 0)
            };
        }



        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }


}
