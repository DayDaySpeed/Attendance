using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;


namespace Attendance.Behaviors
{

    // 实现级联菜单效果的行为，即当鼠标悬停在某个子菜单项上时，自动关闭同级的其他子菜单
    public class CascadeSubmenuBehavior : Behavior<MenuItem>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AttachToChildren(AssociatedObject);
        }

        private void AttachToChildren(MenuItem parent)
        {
            foreach (var item in parent.Items.OfType<MenuItem>())
            {
                item.MouseEnter += OnMouseEnter;

                // 如果子项还有子项，递归绑定
                if (item.HasItems)
                    AttachToChildren(item);
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is MenuItem hoveredItem)
            {
                var parent = ItemsControl.ItemsControlFromItemContainer(hoveredItem);
                if (parent != null)
                {
                    foreach (var item in parent.Items.OfType<MenuItem>())
                    {
                        if (item != hoveredItem)
                            item.IsSubmenuOpen = false;
                    }

                    hoveredItem.IsSubmenuOpen = true;
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            DetachFromChildren(AssociatedObject);
        }

        private void DetachFromChildren(MenuItem parent)
        {
            foreach (var item in parent.Items.OfType<MenuItem>())
            {
                item.MouseEnter -= OnMouseEnter;
                if (item.HasItems)
                    DetachFromChildren(item);
            }
        }
    }
}
