using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;


namespace Attendance.Behaviors
{

    // 当ScrollViewer尺寸变化时，通知绑定的ViewModel进行处理
    public class ScrollViewerSizeChangedBehavior : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SizeChanged += OnSizeChanged;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SizeChanged -= OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AssociatedObject.DataContext is MainViewModel vm)
            {
                vm.LastScrollViewerHeight = e.NewSize.Height;

                vm.DailyPoemVM1?.AdaptPoemToHeight(vm.LastScrollViewerHeight);
                vm.DailyPoemVM2?.AdaptPoemToHeight(vm.LastScrollViewerHeight);
            }
        }
    }
}
