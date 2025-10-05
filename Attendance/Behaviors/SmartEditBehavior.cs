using Attendance.Classes;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Attendance.Behaviors
{
    public class SmartEditBehavior : Behavior<TextBox>
    {
        //智能编辑行为
        private string originalText;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;

            // 监听窗口点击事件
            Window window = Window.GetWindow(AssociatedObject);
            if (window != null)
                window.PreviewMouseDown += OnWindowMouseDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Loaded -= OnLoaded;
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;

            Window window = Window.GetWindow(AssociatedObject);
            if (window != null)
                window.PreviewMouseDown -= OnWindowMouseDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Focus();
            AssociatedObject.SelectAll();

            if (AssociatedObject.DataContext is Cla cla)
                originalText = cla.Name;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (AssociatedObject.DataContext is Cla cla)
            {
                if (e.Key == Key.Enter)
                {
                    cla.IsEditing = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    cla.Name = originalText;
                    cla.IsEditing = false;
                    e.Handled = true;
                }
            }
        }

        private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = Mouse.DirectlyOver as DependencyObject;

            // 如果点击的是 TextBox 或其子元素，则不处理
            if (FindParent<TextBox>(clickedElement) == AssociatedObject)
                return;

            if (AssociatedObject.DataContext is Cla cla)
                cla.IsEditing = false;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;

                DependencyObject parentObj = null;

                // 尝试视觉树
                if (child is Visual || child is Visual3D)
                {
                    parentObj = VisualTreeHelper.GetParent(child);
                }

                // 如果视觉树失败，尝试逻辑树
                if (parentObj == null)
                {
                    parentObj = LogicalTreeHelper.GetParent(child);
                }

                child = parentObj;
            }

            return null;
        }


    }
}
