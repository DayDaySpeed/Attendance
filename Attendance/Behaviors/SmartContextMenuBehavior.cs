using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using System.Windows.Media;

namespace Attendance.Behaviors
{
    public class SmartContextMenuBehavior : Behavior<ListBox>
    {
        public ContextMenu ItemContextMenu { get; set; }
        public ContextMenu EmptyContextMenu { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseRightButtonDown += OnRightClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseRightButtonDown -= OnRightClick;
        }

        private void OnRightClick(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(AssociatedObject);
            var element = AssociatedObject.InputHitTest(point) as DependencyObject;
            var container = FindAncestor<ListBoxItem>(element);

            if (container != null)
            {
                container.IsSelected = true;
                if (ItemContextMenu != null)
                {
                    ItemContextMenu.PlacementTarget = container;
                    ItemContextMenu.IsOpen = true;
                }
            }
            else
            {
                if (EmptyContextMenu != null)
                {
                    EmptyContextMenu.PlacementTarget = AssociatedObject;
                    EmptyContextMenu.IsOpen = true;
                }
            }

            e.Handled = true;
        }

        private T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null && !(current is T))
            {
                current = VisualTreeHelper.GetParent(current);
            }
            return current as T;
        }
    }
}
