using Attendance.Utils;


namespace Attendance.weather
{
    public class PrecipitationItem : ObservableObject
    {
        public string FxTime { get; set; }   // 时间
        public string Precip { get; set; }   // 降水量

        // 字体大小，默认20，可调节
        private double _fontSize = 20;
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }
    }

}
