using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Attendance.Behaviors
{
    public class DragSelectBehavior : Behavior<ListBox>
    {
        private Point _startPoint;
        private SelectionAdorner _adorner;
        private AdornerLayer _adornerLayer;

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
            AssociatedObject.PreviewMouseMove += OnMouseMove;
            AssociatedObject.PreviewMouseLeftButtonUp += OnMouseUp;
            AssociatedObject.MouseLeave += OnMouseLeave;

            Application.Current.MainWindow.PreviewMouseLeftButtonDown += OnGlobalMouseDown;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(AssociatedObject);
            _adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);

            if (_adornerLayer != null)
            {
                _adorner = new SelectionAdorner(AssociatedObject);
                _adornerLayer.Add(_adorner);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _adorner == null) return;

            var pos = e.GetPosition(AssociatedObject);
            pos.X = Math.Max(0, Math.Min(pos.X, AssociatedObject.ActualWidth));
            pos.Y = Math.Max(0, Math.Min(pos.Y, AssociatedObject.ActualHeight));

            var x = Math.Min(pos.X, _startPoint.X);
            var y = Math.Min(pos.Y, _startPoint.Y);
            var width = Math.Abs(pos.X - _startPoint.X);
            var height = Math.Abs(pos.Y - _startPoint.Y);

            var rect = new Rect(x, y, width, height);
            _adorner.Update(rect);

            AssociatedObject.SelectedItems.Clear();
            foreach (var item in AssociatedObject.Items)
            {
                var container = AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container == null) continue;

                var bounds = container.TransformToVisual(AssociatedObject)
                                      .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));

                if (rect.IntersectsWith(bounds))
                {
                    AssociatedObject.SelectedItems.Add(item);
                }
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_adorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_adorner);
                _adorner = null;
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (_adorner != null && _adornerLayer != null)
            {
                _adornerLayer.Remove(_adorner);
                _adorner = null;
            }
        }

        private void OnGlobalMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(AssociatedObject);
            if (pos.X < 0 || pos.Y < 0 || pos.X > AssociatedObject.ActualWidth || pos.Y > AssociatedObject.ActualHeight)
            {
                AssociatedObject.SelectedItems.Clear();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
            AssociatedObject.PreviewMouseMove -= OnMouseMove;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnMouseUp;
            AssociatedObject.MouseLeave -= OnMouseLeave;

            Application.Current.MainWindow.PreviewMouseLeftButtonDown -= OnGlobalMouseDown;
        }
    }
}
