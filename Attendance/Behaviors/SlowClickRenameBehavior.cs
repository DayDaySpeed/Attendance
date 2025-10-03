using Attendance.Classes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace Attendance.Behaviors
{
    public class SlowClickRenameBehavior : Behavior<TextBlock>
    {
        private DispatcherTimer timer;
        private Point clickPosition;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
            AssociatedObject.MouseMove += OnMouseMove;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
            AssociatedObject.MouseMove -= OnMouseMove;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.DataContext is Cla cla)
            {
                clickPosition = e.GetPosition(null);

                timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.2) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    Point current = Mouse.GetPosition(null);
                    if (Math.Abs(current.X - clickPosition.X) < 2 &&
                        Math.Abs(current.Y - clickPosition.Y) < 2)
                    {
                        cla.IsEditing = true;
                    }
                };
                timer.Start();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            timer?.Stop();
        }
    }
}
