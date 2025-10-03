using Attendance.Classes;
using Attendance.View;

using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace Attendance.Animation
{
    public class DoubleRollCallAnimator
    {
        // 控件引用：两个横向滚动区域
        private readonly ScrollViewer originalscrollViewer;               // 原始 ScrollViewer
        private readonly ItemsControl originalitemsControl;               // 原始 ItemsControl
        private readonly ItemsControl leftItemsControl;
        private readonly ItemsControl rightItemsControl;
        private readonly ScrollViewer leftScrollViewer;
        private readonly ScrollViewer rightScrollViewer;
        private readonly Canvas overlayCanvas;
        private readonly FrameworkElement settingsPanel;          // 设置区域面板

        // 数据源
        private readonly RollCallViewModel viewModel;

        // 屏幕尺寸
        private readonly double screenWidth = SystemParameters.PrimaryScreenWidth;
        private readonly double screenHeight = SystemParameters.PrimaryScreenHeight;

        public DoubleRollCallAnimator(
            RollCallViewModel vm,
            ScrollViewer originalscrollViewer,
            ItemsControl originalitemsControl,
            ItemsControl leftItemsControl,
            ItemsControl rightItemsControl,
            ScrollViewer leftScrollViewer,
            ScrollViewer rightScrollViewer,
            FrameworkElement settingsPanel,
            Canvas overlayCanvas)
        {
            viewModel = vm;
            this.originalscrollViewer = originalscrollViewer;
            this.originalitemsControl = originalitemsControl;
            this.leftItemsControl = leftItemsControl;
            this.rightItemsControl = rightItemsControl;
            this.leftScrollViewer = leftScrollViewer;
            this.rightScrollViewer = rightScrollViewer;
            this.settingsPanel = settingsPanel;
            this.overlayCanvas = overlayCanvas;
        }

        /// <summary>
        /// 启动双人对称抽取动画
        /// </summary>
        public async Task StartAsync()
        {
            await FadeOutSettingsPanel();
            await ExpandScrollViewer();

            var shuffled = ShuffleStudents();
            var infiniteList = BuildInfiniteList(shuffled, 1);

            viewModel.LeftScrollingStudents.Clear();
            viewModel.RightScrollingStudents.Clear();

            foreach (var s in infiniteList)
            {
                viewModel.LeftScrollingStudents.Add(s);
                viewModel.RightScrollingStudents.Add(s);
            }

            await Task.Delay(300);
            await FadeOutOldItems();

            // ✅ 同时添加
            await Task.WhenAll(
                LeftAddAsync(shuffled, leftItemsControl, leftScrollViewer),
                RightAddAsync(shuffled, rightItemsControl, rightScrollViewer)
            );
            await Task.Delay(300); // 等待布局稳定

            // ✅ 随机抽取两个学生
            //var selected = shuffled.OrderBy(_ => Guid.NewGuid()).Take(2).ToList();
            var selected = AnimatorService.DrawStudentsWithSettings(viewModel.DisplayedStudents,2,viewModel.SelectedGenderPreference,viewModel.SelectedTailDigit);
            var leftWinner = selected[0];
            var rightWinner = selected[1];
            await Task.Delay(300); // 等待布局稳定

            // ✅ 同时滚动
            await Task.WhenAll(
            ScrollToStudentAsync(leftWinner, leftScrollViewer, leftItemsControl, viewModel.LeftScrollingStudents, isRightToLeft: true ),
            ScrollToStudentAsync(rightWinner, rightScrollViewer, rightItemsControl, viewModel.RightScrollingStudents, isRightToLeft: true)
            );

            // ✅ 展示浮层
            await ShowSymmetricFloatingAsync(leftWinner, rightWinner);
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
                From = originalscrollViewer.ActualWidth,
                To = screenWidth,
                Duration = TimeSpan.FromSeconds(1.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            originalscrollViewer.BeginAnimation(FrameworkElement.WidthProperty, expandAnim);
            await Task.Delay(1200);

            originalscrollViewer.SetValue(Grid.ColumnSpanProperty, 2);
            originalscrollViewer.HorizontalAlignment = HorizontalAlignment.Center;
            viewModel.StudentPanelWidth = screenWidth;
            originalscrollViewer.Width = screenWidth;
        }

        // 旧 ItemsControl 中的元素逐个淡出
        private async Task FadeOutOldItems()
        {
            for (int i = 0; i < originalitemsControl.Items.Count; i++)
            {
                var container = originalitemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    originalitemsControl.UpdateLayout();
                    originalscrollViewer.UpdateLayout();


                    // 淡出动画
                    var fade = new DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.3),
                        BeginTime = TimeSpan.Zero
                    };
                    container.BeginAnimation(UIElement.OpacityProperty, fade);

                    await Task.Delay(50);
                }

                // ✅ 检查是否还有元素在视口中
                if (!AnimatorService.AnyVisibleElementInViewport(originalitemsControl, originalscrollViewer))
                {
                    originalscrollViewer.Visibility = Visibility.Collapsed;
                    break; // 屏幕中已无可见元素，提前结束
                }
            }

        }

        /// <summary>
        /// 打乱学生顺序
        /// </summary>
        private List<Student> ShuffleStudents()
        {
            return viewModel.DisplayedStudents.OrderBy(x => Guid.NewGuid()).ToList();
        }

        /// <summary>
        /// 构建无限滚动列表
        /// </summary>
        private List<Student> BuildInfiniteList(List<Student> original, int repeatCount)
        {
            var result = new List<Student>();
            for (int i = 0; i < repeatCount; i++)
                result.AddRange(original);
            return result;
        }

        public async Task ScrollToStudentAsync(
            Student targetStudent,
            ScrollViewer scrollViewer,
            ItemsControl itemsControl,
            ObservableCollection<Student> students,
            bool isRightToLeft,
            bool enablePreheat = true,
            int preheatDurationMs = 5000)
        {
            itemsControl.UpdateLayout();
            scrollViewer.UpdateLayout();

            int index = students.IndexOf(targetStudent);
            if (index < 0) return;

            FrameworkElement container = null;
            int retry = 0;
            while (container == null && retry < 20)
            {
                itemsControl.UpdateLayout();
                scrollViewer.UpdateLayout();
                container = itemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                await Task.Delay(50);
                retry++;
            }
            if (container == null) return;

            Point position = container.TransformToAncestor(scrollViewer).Transform(new Point(0, 0));
            double elementCenter = position.X + container.ActualWidth / 2;
            double targetOffset = elementCenter - scrollViewer.ViewportWidth / 2;

            // ✅ 预热阶段（先快后慢）
            if (enablePreheat)
            {
                var preheatTcs = new TaskCompletionSource<bool>();
                var preheatTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
                double preheatOffset = isRightToLeft ? scrollViewer.ScrollableWidth : scrollViewer.HorizontalOffset;
                double initialSpeed = 40;
                int ticks = 0;
                int maxTicks = preheatDurationMs / 30;

                preheatTimer.Tick += (s, e) =>
                {
                    double progressRatio = (double)ticks / maxTicks;
                    double scrollSpeed = initialSpeed * (1 - progressRatio); // 从快到慢

                    preheatOffset += isRightToLeft ? -scrollSpeed : scrollSpeed;
                    scrollViewer.ScrollToHorizontalOffset(preheatOffset);
                    ticks++;

                    if (ticks >= maxTicks)
                    {
                        preheatTimer.Stop();
                        preheatTcs.SetResult(true);
                    }
                };

                preheatTimer.Start();
                await preheatTcs.Task;
            }


            // ✅ 精准滚动阶段
            double currentOffset = scrollViewer.HorizontalOffset;
            double scrollSpeed = 30;
            double slowSpeed = 5;
            double finalSpeed = 1.5;
            double decelerationZone = 600;

            var tcs = new TaskCompletionSource<bool>();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };

            timer.Tick += (s, e) =>
            {
                double distance = Math.Abs(targetOffset - currentOffset);

                if (distance <= decelerationZone)
                    scrollSpeed = Math.Max(slowSpeed, scrollSpeed * 0.95);

                if (distance <= 100)
                    scrollSpeed = finalSpeed;

                currentOffset += isRightToLeft ? -scrollSpeed : scrollSpeed;

                if ((isRightToLeft && currentOffset <= targetOffset) ||
                    (!isRightToLeft && currentOffset >= targetOffset))
                {
                    scrollViewer.ScrollToHorizontalOffset(targetOffset);
                    timer.Stop();
                    tcs.SetResult(true);
                    Dispatcher.CurrentDispatcher.InvokeAsync(() =>
                    {
                        scrollViewer.ScrollToHorizontalOffset(targetOffset);
                    }, DispatcherPriority.Background);

                    return;
                }

                scrollViewer.ScrollToHorizontalOffset(currentOffset);
            };

            timer.Start();
            await tcs.Task;
        }




        /// <summary>
        /// 滑入
        /// </summary>
        private async Task LeftAddAsync(List<Student> shuffled, ItemsControl itemsControl, ScrollViewer scrollViewer)
        {
            // 清空旧数据并显示滚动区域
            viewModel.LeftScrollingStudents.Clear();
            leftScrollViewer.Visibility = Visibility.Visible;

            for (int i = 0; i < shuffled.Count; i++)
            {
                var student = shuffled[i];
                viewModel.LeftScrollingStudents.Add(student);

                await Task.Delay(30); // 控制节奏
                itemsControl.UpdateLayout();
                scrollViewer.UpdateLayout();

                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    container.Opacity = 1; // 直接显示，无动画

                    // 滚动到当前元素位置
                    var pos = container.TransformToAncestor(scrollViewer).Transform(new Point(0, 0));
                    scrollViewer.ScrollToHorizontalOffset(pos.X - 60);
                }
            }
        }
        private async Task RightAddAsync(List<Student> shuffled, ItemsControl itemsControl, ScrollViewer scrollViewer)
        {
            // 清空旧数据并显示滚动区域
            viewModel.RightScrollingStudents.Clear();
            rightScrollViewer.Visibility = Visibility.Visible;

            for (int i = 0; i < shuffled.Count; i++)
            {
                var student = shuffled[i];
                viewModel.RightScrollingStudents.Add(student);

                await Task.Delay(50); // 控制节奏
                itemsControl.UpdateLayout();
                scrollViewer.UpdateLayout();

                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    container.Opacity = 1; // 直接显示，无动画

                    // 滚动到当前元素位置
                    var pos = container.TransformToAncestor(scrollViewer).Transform(new Point(0, 0));
                    scrollViewer.ScrollToHorizontalOffset(pos.X - 60);
                }
            }
        }




        private async Task ShowSymmetricFloatingAsync(Student leftStudent, Student rightStudent)
        {
            // 获取左侧卡片容器
            int leftIndex = viewModel.LeftScrollingStudents.IndexOf(leftStudent);
            if (leftIndex < 0) return;
            leftItemsControl.UpdateLayout();
            var leftContainer = leftItemsControl.ItemContainerGenerator.ContainerFromIndex(leftIndex) as FrameworkElement;
            if (leftContainer == null) return;
            var leftOriginalBorder = AnimatorService.FindVisualChild<Border>(leftContainer);
            if (leftOriginalBorder == null) return;

            // 获取右侧卡片容器
            int rightIndex = viewModel.RightScrollingStudents.IndexOf(rightStudent);
            if (rightIndex < 0) return;
            rightItemsControl.UpdateLayout();
            var rightContainer = rightItemsControl.ItemContainerGenerator.ContainerFromIndex(rightIndex) as FrameworkElement;
            if (rightContainer == null) return;
            var rightOriginalBorder = AnimatorService.FindVisualChild<Border>(rightContainer);
            if (rightOriginalBorder == null) return;

            // 克隆左卡片
            var leftClone = new Border
            {
                Width = 120,
                Height = 120,
                CornerRadius = new CornerRadius(20),
                Background = leftOriginalBorder.Background,
                Effect = leftOriginalBorder.Effect,
                Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = {
                new TextBlock
                {
                    Text = leftStudent.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
                }
            };

            // 克隆右卡片
            var rightClone = new Border
            {
                Width = 120,
                Height = 120,
                CornerRadius = new CornerRadius(20),
                Background = rightOriginalBorder.Background,
                Effect = rightOriginalBorder.Effect,
                Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = {
                new TextBlock
                {
                    Text = rightStudent.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
                }
            };

            // 定位
            double spacing = 40;
            Canvas.SetLeft(leftClone, (screenWidth / 2) - leftClone.Width - spacing);
            Canvas.SetTop(leftClone, (screenHeight - leftClone.Height) / 2);

            Canvas.SetLeft(rightClone, (screenWidth / 2) + spacing);
            Canvas.SetTop(rightClone, (screenHeight - rightClone.Height) / 2);

            overlayCanvas.Children.Add(leftClone);
            overlayCanvas.Children.Add(rightClone);

            // 动画
            AnimateFloatingCard(leftClone);
            AnimateFloatingCard(rightClone);

            await Task.Delay(1500);
            //overlayCanvas.Children.Remove(leftClone);
            //overlayCanvas.Children.Remove(rightClone);
        }
        /// <summary>
        /// 启动卡片的旋转、放大、浮起动画
        /// </summary>
        private void AnimateFloatingCard(Border card)
        {
            var transformGroup = new TransformGroup();
            var rotate = new RotateTransform(0);
            var scale = new ScaleTransform(1, 1);
            var translate = new TranslateTransform(0, 0);
            transformGroup.Children.Add(rotate);
            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);
            card.RenderTransform = transformGroup;
            card.RenderTransformOrigin = new Point(0.5, 0.5);

            // 旋转动画
            rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            });

            // 放大动画
            var scaleAnim = new DoubleAnimation(1, 2.2, TimeSpan.FromSeconds(0.5));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // 浮起动画
            var floatAnim = new DoubleAnimation(0, -40, TimeSpan.FromSeconds(0.5));
            translate.BeginAnimation(TranslateTransform.YProperty, floatAnim);
        }

    } }