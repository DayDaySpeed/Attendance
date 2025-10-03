using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Attendance.Converters
{
    public class AirqualityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Brushes.Gray;
            string strValue = value.ToString();


            if (int.TryParse(value.ToString(), out int aqi))
            {
                if (aqi <= 50) return Brushes.Green;       // 优
                if (aqi <= 100) return Brushes.Yellow;     // 良
                if (aqi <= 150) return Brushes.Orange;     // 轻度污染
                if (aqi <= 200) return Brushes.Red;        // 中度污染
                if (aqi <= 300) return Brushes.Purple;     // 重度污染
                return Brushes.Brown;                      // 严重污染
            }

            // 如果是 Category（中文）
            switch (strValue)
            {
                case "低": return Brushes.Green;
                case "中": return Brushes.Yellow;
                case "高": return Brushes.Orange;
                case "甚高": return Brushes.Red;
                case "严重": return Brushes.Purple;
                default: return Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
