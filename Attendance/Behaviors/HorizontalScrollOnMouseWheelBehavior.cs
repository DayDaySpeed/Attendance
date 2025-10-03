using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

//横向滚动
namespace Attendance.Behaviors
{
    //实现鼠标滚轮横向滚动的行为
    public class HorizontalScrollOnMouseWheelBehavior : Behavior<ScrollViewer>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.ScrollToHorizontalOffset(AssociatedObject.HorizontalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }
}
