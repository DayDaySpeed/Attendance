using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Attendance.Utils
{
    public class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = "")
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        /// <summary>
        /// 通用属性设置器，支持变更前通知和变更后通知
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            OnPropertyChanging(propertyName);
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 异步属性设置器（无 ref），适用于 Task 类型或异步场景
        /// </summary>
        protected async Task<bool> SetPropertyAsync<T>(
            T currentValue,
            T newValue,
            Action<T> setter,
            [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
                return false;

            OnPropertyChanging(propertyName);
            setter(newValue);
            OnPropertyChanged(propertyName);

            if (newValue is Task task)
            {
                await task;
                OnPropertyChanged(propertyName); // 再次通知以刷新状态
            }

            return true;
        }

        /// <summary>
        /// 批量通知多个属性变更
        /// </summary>
        protected void NotifyProperties(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                OnPropertyChanging(name);
                OnPropertyChanged(name);
            }
        }

        /// <summary>
        /// 通知所有属性变更（用于大范围刷新）
        /// </summary>
        protected void NotifyAllProperties()
        {
            OnPropertyChanging(string.Empty);
            OnPropertyChanged(string.Empty);
        }
    }
}


