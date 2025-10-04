using Attendance.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Attendance.Animation
{
    public static class AnimatorService
    {
        /// <summary>
        /// 根据用户设置从学生集合中抽取若干名不重复学生，支持性别筛选和尾号加权。
        /// </summary>
        public static List<Student> DrawStudentsWithSettings(
            ObservableCollection<Student> allStudents,
            int count,
            string genderPreference,
            int tailDigitPreference)
        {
            if (allStudents == null || allStudents.Count == 0 || count <= 0)
                return new List<Student>();

            // 1️⃣ 筛选性别
            var filtered = allStudents.Where(s =>
                genderPreference == "全部" ||
                (genderPreference == "男" && s.Gender == Student.GenderEnum.male) ||
                (genderPreference == "女" && s.Gender == Student.GenderEnum.female)).ToList();

            if (filtered.Count == 0)
                return new List<Student>();

            // 2️⃣ 构建加权池
            var weightedPool = new List<Student>();
            foreach (var student in filtered)
            {
                int weight = 1;

                if (tailDigitPreference >= 0)
                {
                    int tail = (int)(student.StudentNumber % 10);
                    if (tail == tailDigitPreference)
                        weight += 10;
                }

                for (int i = 0; i < weight; i++)
                    weightedPool.Add(student);
            }

            // 3️⃣ 随机抽取不重复学生
            var random = new Random();
            var winners = new List<Student>();
            var usedIds = new HashSet<int>();

            while (winners.Count < count && weightedPool.Count > 0)
            {
                int index = random.Next(weightedPool.Count);
                var candidate = weightedPool[index];

                if (!usedIds.Contains(candidate.id))
                {
                    winners.Add(candidate);
                    usedIds.Add(candidate.id);
                }

                // 移除所有该学生的权重副本
                weightedPool.RemoveAll(s => s.id == candidate.id);
            }

            return winners;
        }

        /// <summary>
        /// 在视觉树中查找指定类型的子元素。
        /// </summary>
        public static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T found)
                    return found;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 判断 ItemsControl 中是否有元素在 ScrollViewer 的可视区域内。
        /// </summary>
        public static bool AnyVisibleElementInViewport(ItemsControl itemsControl, ScrollViewer scrollViewer)
        {
            if (itemsControl == null || scrollViewer == null)
                return false;

            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null && container.Opacity > 0.1)
                {
                    var transform = container.TransformToAncestor(scrollViewer);
                    var position = transform.Transform(new Point(0, 0));

                    double elementTop = position.Y;
                    double elementBottom = elementTop + container.ActualHeight;
                    double viewportTop = scrollViewer.VerticalOffset;
                    double viewportBottom = viewportTop + scrollViewer.ViewportHeight;

                    if (elementBottom > viewportTop && elementTop < viewportBottom)
                        return true;
                }
            }

            return false;
        }
    }

}
