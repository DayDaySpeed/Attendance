using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Attendance.Converters
{
    public class IconKeyToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && !string.IsNullOrEmpty(key))
            {
                if (Application.Current.Resources.Contains(key))
                {
                    if (Application.Current.Resources[key] is DrawingImage di)
                    {
                        var clone = di.Clone();

                        Brush brush = null;

                        if (parameter is string paramStr && !string.IsNullOrWhiteSpace(paramStr))
                        {
                            // 如果包含逗号，认为是渐变色
                            var colors = paramStr.Split(',')
                                                 .Select(c => c.Trim())
                                                 .Where(c => !string.IsNullOrEmpty(c))
                                                 .ToList();

                            if (colors.Count > 1)
                            {
                                var gradient = new LinearGradientBrush
                                {
                                    StartPoint = new Point(0, 0),
                                    EndPoint = new Point(1, 1)
                                };

                                double offsetStep = 1.0 / (colors.Count - 1);
                                for (int i = 0; i < colors.Count; i++)
                                {
                                    try
                                    {
                                        var color = (Color)ColorConverter.ConvertFromString(colors[i]);
                                        gradient.GradientStops.Add(new GradientStop(color, i * offsetStep));
                                    }
                                    catch { }
                                }
                                brush = gradient;
                            }
                            else
                            {
                                // 单色
                                try
                                {
                                    var color = (Color)ColorConverter.ConvertFromString(colors.First());
                                    brush = new SolidColorBrush(color);
                                }
                                catch
                                {
                                    brush = Brushes.Black;
                                }
                            }
                        }

                        brush ??= Brushes.Black;

                        ApplyBrush(clone.Drawing, brush);

                        return clone;
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private void ApplyBrush(Drawing drawing, Brush brush)
        {
            if (drawing is GeometryDrawing gd)
            {
                gd.Brush = brush;
            }
            else if (drawing is DrawingGroup dg)
            {
                foreach (var child in dg.Children)
                {
                    ApplyBrush(child, brush);
                }
            }
        }
    }

}
