using Attendance.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.weather
{
    public class WeatherInfo : ObservableObject
    {
        public string IconKey { get; set; }     // icons.xaml 的 Key
        public string Category { get; set; }    // "【天气】" 或 "【生活】"
        public string Description { get; set; } // 具体描述


        // 字体大小，默认20，可调节
        private double _fontSize = 20;
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }


        // 重写 ToString 方法，方便走马灯展示
        public override string ToString()
        {
            return $"{Category}{Description}";
        }
    }

}
