using Attendance.Theme;
using System.Windows.Media;

namespace Attendance.Theme
{
    public static class ThemePresets
    {

        ////白字黑底
        public static ThemePayload BlackBackground => new() { Background = Brushes.Black, Foreground = Brushes.White };
        ////黑字白底
        public static ThemePayload WhiteBackground => new() { Background = Brushes.White , Foreground = Brushes.Black };
       
    }

}
