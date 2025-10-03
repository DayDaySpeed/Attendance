using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Attendance.Converters

{
    public class IfVisibility : IValueConverter
    {
        /// <summary>
        /// 转换逻辑
        /// </summary>
        /// <param name="value">集合对象（需实现ICollection接口）</param>
        /// <param name="targetType">目标类型（Visibility）</param>
        /// <param name="parameter">参数：传入"Inverse"表示反转结果</param>
        /// <param name="culture">区域信息</param>
        /// <returns>Visibility枚举值</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasItems = false;

            if (value is int count)
            {
                hasItems = count > 0;
            }

            if (parameter?.ToString() == "Inverse")
            {
                hasItems = !hasItems;
            }

            return hasItems ? Visibility.Visible : Visibility.Collapsed;
        }

        // 反向转换（单向绑定场景无需实现）
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("不支持反向转换");
        }
    }
}
