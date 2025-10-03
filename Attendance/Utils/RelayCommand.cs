using System;
using System.Windows.Input;

namespace Attendance.Utils
{
    public class RelayCommand : ICommand
    {
        // 私有字段：存储命令的执行逻辑（无返回值、无参数的方法）
        private readonly Action _execute;

        // 私有字段：存储命令是否可执行的判断逻辑（返回 bool、无参数的方法，可选）
        private readonly Func<bool> _canExecute;

        // 实现 ICommand 接口的事件：当命令可执行状态变化时触发，通知 UI 更新控件状态（如按钮启用/禁用）
        public event EventHandler CanExecuteChanged;

        // 构造函数：初始化命令的执行逻辑和可执行判断逻辑
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            // 检查执行逻辑是否为 null，若为 null 则抛出异常（确保命令必须有执行逻辑）
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;  // 可执行逻辑可为 null（默认认为命令始终可执行）
        }

        // 实现 ICommand 接口：判断命令当前是否可执行
        public bool CanExecute(object parameter)
        {
            bool result = _canExecute == null || _canExecute();
            Console.WriteLine($"CanExecute: {result}"); // 检查是否返回 true
            return result;
        }

        // 实现 ICommand 接口：执行命令的核心逻辑
        public void Execute(object parameter)
        {
            Console.WriteLine(_execute);
            _execute();  // 调用存储的执行逻辑（无参数）
        }

        // 手动触发 CanExecuteChanged 事件的方法：当可执行状态变化时调用，通知 UI 更新
        public void RaiseCanExecuteChanged()
        {
            // 若有订阅者，则触发事件（空条件运算符 ?. 避免空引用异常）
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // 支持带参数的命令版本：泛型 T 表示参数类型
    public class RelayCommand<T> : ICommand
    {
        // 私有字段：存储带参数的执行逻辑（无返回值、参数类型为 T）
        private readonly Action<T> _execute;

        // 私有字段：存储带参数的可执行判断逻辑（返回 bool、参数类型为 T，可选）
        private readonly Func<T, bool> _canExecute;

        // 实现 ICommand 接口的事件：同非泛型版本，用于通知可执行状态变化
        public event EventHandler CanExecuteChanged;

        // 构造函数：初始化带参数的执行逻辑和可执行判断逻辑
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            // 检查执行逻辑是否为 null，确保必须有执行逻辑
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;  // 可执行逻辑可为 null
        }

        // 实现 ICommand 接口：判断带参数的命令是否可执行
        public bool CanExecute(object parameter)
        {
            // 若 _canExecute 为 null，默认返回 true；否则将参数转换为 T 类型并执行判断
            return _canExecute == null || _canExecute((T)parameter);
        }

        // 实现 ICommand 接口：执行带参数的命令逻辑
        public void Execute(object parameter)
        {
            _execute((T)parameter);  // 将参数转换为 T 类型并调用执行逻辑
        }

        // 手动触发可执行状态变化事件，同非泛型版本
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}