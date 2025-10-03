using Attendance.Classes;
using Attendance.View;
using PP.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Attendance.Animation
{
    public class NormalRollCallAnimator
    {
        private readonly RollCallViewModel vm;
        private readonly ObservableCollection<Student> students;
        private readonly ItemsControl itemsControl;
        private readonly ScrollViewer scrollViewer;
        private readonly int selectionCount;
        private readonly List<Student> selectedStudents;
        private readonly FrameworkElement settingsPanel;
        private readonly Canvas overlayCanvas;
        private readonly double screenWidth;
        private readonly double screenHeight;

        public NormalRollCallAnimator(RollCallViewModel vm,
                                      ItemsControl itemsControl,
                                      ScrollViewer scrollViewer,
                                      FrameworkElement settingpanel,
                                      Canvas overlayCanvas,
                                      int selectionCount)
        {
            this.vm = vm;
            this.students = vm.DisplayedStudents;
            this.itemsControl = itemsControl;
            this.scrollViewer = scrollViewer;
            this.settingsPanel = settingpanel;
            this.overlayCanvas = overlayCanvas;
            this.selectionCount = Math.Max(4, Math.Min(10, selectionCount));
            //this.selectedStudents = students.OrderBy(_ => Guid.NewGuid()).Take(this.selectionCount).ToList();
            this.selectedStudents = AnimatorService.DrawStudentsWithSettings(vm.DisplayedStudents,selectionCount,vm.SelectedGenderPreference,vm.SelectedTailDigit);
            screenWidth = SystemParameters.PrimaryScreenWidth;
            screenHeight = SystemParameters.PrimaryScreenHeight;
        }

        public async Task StartAsync()
        {
            await FadeOutSettingsPanel();                  // 淡出设置区域
            await ExpandScrollViewer();                    // ScrollViewer 宽度扩展动画

            itemsControl.UpdateLayout();
            scrollViewer.UpdateLayout();


            double itemHeight = 120 + 20; // 卡片高度 + Margin
            double viewportHeight = scrollViewer.ViewportHeight;
            double totalHeight = itemsControl.ActualHeight;
            double maxOffset = Math.Max(0, totalHeight - viewportHeight);

            int totalSteps = students.Count;
            int intervalMs = 150;
            int itemsPerRow = CalculateItemsPerRow(); // 横向 WrapPanel 每行能放多少个
            var selectedSet = new HashSet<Student>(selectedStudents);

            double currentOffset = 0;
            int lastRowIndex = -1;

            for (int i = 0; i < totalSteps; i++)
            {
                var student = students[i];
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;

                // 按行推进滚动
                int currentRow = i / itemsPerRow;
                if (currentRow != lastRowIndex)
                {
                    currentOffset = Math.Min(currentRow * itemHeight, maxOffset);
                    scrollViewer.ScrollToVerticalOffset(currentOffset);
                    lastRowIndex = currentRow;
                }

                // 高亮当前卡片
                var border = FindVisualChild<Border>(container);
                if (border != null)
                {
                    // 1️⃣ 设置发光阴影效果（更亮、更大、更明显）
                    border.Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(255, 255, 0), // 明亮黄
                        BlurRadius = 40,
                        ShadowDepth = 0,
                        Opacity = 0.9
                    };

                    // 2️⃣ 设置边框颜色和厚度
                    border.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    border.BorderThickness = new Thickness(3);

                    // 3️⃣ 创建边框闪烁动画
                    var blink = new ColorAnimation
                    {
                        From = Colors.Yellow,
                        To = Colors.Transparent,
                        Duration = TimeSpan.FromMilliseconds(300),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(3)
                    };

                    Storyboard.SetTarget(blink, border);
                    Storyboard.SetTargetProperty(blink, new PropertyPath("(Border.BorderBrush).(SolidColorBrush.Color)"));

                    // 4️⃣ 创建卡片放大动画
                    var scale = new ScaleTransform(1.0, 1.0);
                    border.RenderTransform = scale;
                    border.RenderTransformOrigin = new Point(0.5, 0.5);

                    var scaleAnim = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.2,
                        Duration = TimeSpan.FromMilliseconds(300),
                        AutoReverse = true,
                        RepeatBehavior = new RepeatBehavior(2)
                    };

                    // 5️⃣ 组合动画并启动
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(blink);
                    storyboard.Begin();

                    scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
                }

                await Task.Delay(intervalMs);

                // 抽中者打勾标记
                if (selectedSet.Contains(student))
                {
                    var checkMark = new TextBlock
                    {
                        Text = "✔",
                        FontSize = 32,
                        Foreground = Brushes.LimeGreen,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 5, 10, 0)
                    };

                    if (border?.Child is StackPanel stack)
                    {
                        stack.Children.Add(checkMark);
                    }

                    await Task.Delay(300);
                }

                // 清除高亮
                if (border != null)
                    border.Effect = null;
            }
            scrollViewer.Visibility = Visibility.Collapsed;
            await ShowSelectedCardsOnCanvasAsync(overlayCanvas, itemsControl, selectedStudents);
        }
        //计算每行元素
        private int CalculateItemsPerRow()
        {
            double panelWidth = itemsControl.ActualWidth;
            double itemWidth = 120 + 20; // 卡片宽度 + Margin
            return Math.Max(1, (int)(panelWidth / itemWidth));
        }

        // 淡出设置区域动画
        private async Task FadeOutSettingsPanel()
        {
            var fadeOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.5) };
            settingsPanel.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            await Task.Delay(500);
            settingsPanel.Visibility = Visibility.Collapsed;
        }

        // ScrollViewer 宽度扩展动画
        private async Task ExpandScrollViewer()
        {
            var expandAnim = new DoubleAnimation
            {
                From = scrollViewer.ActualWidth,
                To = screenWidth,
                Duration = TimeSpan.FromSeconds(1.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            scrollViewer.BeginAnimation(FrameworkElement.WidthProperty, expandAnim);
            await Task.Delay(1200);

            scrollViewer.SetValue(Grid.ColumnSpanProperty, 2);
            scrollViewer.HorizontalAlignment = HorizontalAlignment.Center;
            vm.StudentPanelWidth = screenWidth;
            scrollViewer.Width = screenWidth;
        }
        public async Task ShowSelectedCardsOnCanvasAsync(Canvas overlayCanvas, ItemsControl itemsControl, List<Student> selectedStudents)
        {
            if (overlayCanvas == null || itemsControl == null || selectedStudents == null || selectedStudents.Count == 0)
                return;

            overlayCanvas.UpdateLayout();
            overlayCanvas.Children.Clear();
            itemsControl.UpdateLayout();

            const int maxPerRow = 5;
            const double cardWidth = 120;
            const double spacing = 20;
            double stepX = cardWidth + spacing;
            double stepY = cardWidth + spacing;

            int totalRows = (int)Math.Ceiling(selectedStudents.Count / (double)maxPerRow);
            double canvasWidth = overlayCanvas.ActualWidth;
            double canvasHeight = overlayCanvas.ActualHeight;

            for (int i = 0; i < selectedStudents.Count; i++)
            {
                var student = selectedStudents[i];
                int index = itemsControl.Items.IndexOf(student);
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                if (container == null) continue;

                var originalBorder = FindVisualChild<Border>(container);
                if (originalBorder == null) continue;

                var clonedBorder = CloneBorder(originalBorder);

                // 计算行列位置
                int row = i / maxPerRow;
                int col = i % maxPerRow;

                int cardsInThisRow = Math.Min(maxPerRow, selectedStudents.Count - row * maxPerRow);
                double rowWidth = cardsInThisRow * stepX;
                double offsetRight = 250; // 可调节
                double startX = (canvasWidth - rowWidth) / 2 + offsetRight;

                double targetX = startX + col * stepX;
                double targetY = (canvasHeight - totalRows * stepY) / 2 + row * stepY;

                // 初始位置在左侧外部
                Canvas.SetLeft(clonedBorder, -150);
                Canvas.SetTop(clonedBorder, targetY);
                overlayCanvas.Children.Add(clonedBorder);

                AnimateCard(clonedBorder, targetX, i * 200);

            }

            await Task.Delay(selectedStudents.Count * 200 + 1000);
        }




        //克隆 Border 方法（复制视觉内容）
        private Border CloneBorder(Border original)
        {
            var cloned = new Border
            {
                Width = original.Width,
                Height = original.Height,
                CornerRadius = original.CornerRadius,
                Background = original.Background,
                Effect = original.Effect,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new TransformGroup
                {
                    Children = new TransformCollection
            {
                new ScaleTransform(1, 1),
                new RotateTransform(0)
            }
                }
            };

            if (original.Child is StackPanel originalStack)
            {
                var clonedStack = new StackPanel
                {
                    VerticalAlignment = originalStack.VerticalAlignment,
                    HorizontalAlignment = originalStack.HorizontalAlignment
                };

                foreach (var child in originalStack.Children)
                {
                    if (child is TextBlock tb)
                    {
                        clonedStack.Children.Add(new TextBlock
                        {
                            Text = tb.Text,
                            FontSize = tb.FontSize,
                            FontWeight = tb.FontWeight,
                            Foreground = tb.Foreground,
                            HorizontalAlignment = tb.HorizontalAlignment
                        });
                    }
                }

                cloned.Child = clonedStack;
            }

            return cloned;
        }


        //动画方法 AnimateCard（弹跳 + 旋转 + 光效）
        private void AnimateCard(UIElement element, double targetX, int delayMs)
        {
            var storyboard = new Storyboard();

            // 飞入动画（Canvas.Left）
            var move = new DoubleAnimation
            {
                From = -150,
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(600),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new BounceEase { Bounces = 2, Bounciness = 3, EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(move, element);
            Storyboard.SetTargetProperty(move, new PropertyPath("(Canvas.Left)"));
            storyboard.Children.Add(move);

            // 旋转动画
            var rotate = new DoubleAnimation
            {
                From = -30,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(rotate, element);
            Storyboard.SetTargetProperty(rotate, new PropertyPath("RenderTransform.Children[1].Angle"));
            storyboard.Children.Add(rotate);

            // 闪烁动画（Opacity）
            var flicker = new DoubleAnimation
            {
                From = 0.6,
                To = 1.0,
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(TimeSpan.FromSeconds(1)),
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };
            Storyboard.SetTarget(flicker, element);
            Storyboard.SetTargetProperty(flicker, new PropertyPath("Opacity"));
            storyboard.Children.Add(flicker);

            // 渐显动画
            element.Opacity = 0;
            var fade = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(delayMs)
            };
            Storyboard.SetTarget(fade, element);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Opacity"));
            storyboard.Children.Add(fade);

            storyboard.Begin();
        }








        public List<Student> GetSelectedStudents() => selectedStudents;

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }
    }


}
