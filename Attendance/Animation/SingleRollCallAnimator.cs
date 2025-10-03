using Attendance.Classes;
using Attendance.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Attendance.Animation
{
    public class SingleRollCallAnimator
    {
        // 控件引用
        private readonly ScrollViewer scrollViewer;               // 原始 ScrollViewer
        private readonly ScrollViewer scrollingViewer;            // 横向滚动 ScrollViewer
        private readonly ItemsControl itemsControl;               // 原始 ItemsControl
        private readonly ItemsControl scrollingItemsControl;      // 横向 ItemsControl
        private readonly RollCallViewModel viewModel;             // 绑定的 ViewModel
        private readonly FrameworkElement settingsPanel;          // 设置区域面板

        // 构建一个重复多次的学生列表，模拟无限滚动
        private List<Student> BuildInfiniteList(List<Student> original, int repeatCount)
        {
            var result = new List<Student>();
            for (int i = 0; i < repeatCount; i++)
                result.AddRange(original);
            return result;
        }

        private readonly Canvas overlayCanvas; // 浮层动画容器

        // 屏幕尺寸
        private readonly double screenWidth;
        private readonly double screenHeight;

        // 构造函数：注入所有依赖控件和数据
        public SingleRollCallAnimator(
            ScrollViewer viewer,
            ScrollViewer scrollingViewer,
            ItemsControl control,
            ItemsControl scrollingControl,
            RollCallViewModel vm,
            FrameworkElement settingsPanel,
            Canvas overlayCanvas)
        {
            scrollViewer = viewer;
            this.scrollingViewer = scrollingViewer;
            itemsControl = control;
            scrollingItemsControl = scrollingControl;

            viewModel = vm;
            this.settingsPanel = settingsPanel;
            screenWidth = SystemParameters.PrimaryScreenWidth;
            screenHeight = SystemParameters.PrimaryScreenHeight;
            this.overlayCanvas = overlayCanvas;
        }

        // 主动画入口方法
        public async Task StartAsync()
        {
            await RunSingleSelectionAnimation();
        }
        // 单人抽取动画流程
        private async Task RunSingleSelectionAnimation()
        {

            await FadeOutSettingsPanel();                  // 淡出设置区域
            await ExpandScrollViewer();                    // ScrollViewer 宽度扩展动画


            var shuffled = ShuffleStudents();                   // 打乱学生顺序
            var infiniteList = BuildInfiniteList(shuffled, 5); // 复制 5 次
            viewModel.ScrollingStudents.Clear();
            foreach (var s in infiniteList)
                viewModel.ScrollingStudents.Add(s);


            await Task.Delay(300);
            itemsControl.UpdateLayout();

            await FadeOutOldItems();                       // 旧元素逐个淡出
            await SmoothMarqueeAddAsync(shuffled);         // 新元素逐个淡入

            // 抽取逻辑放在动画之后
            //var random = new Random();
            //var winner = viewModel.ScrollingStudents[random.Next(viewModel.ScrollingStudents.Count)];
            var winners = AnimatorService.DrawStudentsWithSettings(viewModel.ScrollingStudents,viewModel.SelectedCount,viewModel.SelectedGenderPreference,viewModel.SelectedTailDigit);
            // 只取第一个作为滚动展示
            var winner = winners.FirstOrDefault();
            if (winner != null)
            { // 启动抽卡式动画
                await StartMarqueeScrollAsync(winner);
                // 高亮抽中的学生
                await ShowWinnerFloatingAsync(winner);
            }
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
            viewModel.StudentPanelWidth = screenWidth;
            scrollViewer.Width = screenWidth;
        }

        // 打乱学生顺序
        private List<Student> ShuffleStudents()
        {
            return viewModel.DisplayedStudents.OrderBy(x => Guid.NewGuid()).ToList();
        }

        // 旧 ItemsControl 中的元素逐个淡出
        private async Task FadeOutOldItems()
        {
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    itemsControl.UpdateLayout();
                    scrollViewer.UpdateLayout();


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
                if (!AnimatorService.AnyVisibleElementInViewport(itemsControl, scrollViewer))
                {
                    scrollViewer.Visibility = Visibility.Collapsed;
                    break; // 屏幕中已无可见元素，提前结束
                }
            }
        }


        // 将打乱后的学生列表复制到新集合，并显示横向滚动区域
        private async Task SmoothMarqueeAddAsync(List<Student> shuffled)
        {
            //先清理后显示
            viewModel.ScrollingStudents.Clear();
            scrollingViewer.Visibility = Visibility.Visible;
            //更新
            scrollingItemsControl.UpdateLayout();
            scrollingViewer.UpdateLayout();

            for (int i = 0; i < shuffled.Count; i++)
            {
                var student = shuffled[i];
                viewModel.ScrollingStudents.Add(student);

                await Task.Delay(50); // 等待 UI 更新
                scrollingItemsControl.UpdateLayout();
                scrollingViewer.UpdateLayout();

                var container = scrollingItemsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    // 初始状态：透明 + 向右偏移
                    container.Opacity = 0;
                    var transform = new TranslateTransform { X = 100 };
                    container.RenderTransform = transform;

                    // 滚动到新元素位置
                    var pos = container.TransformToAncestor(scrollingViewer).Transform(new Point(0, 0));
                    scrollingViewer.ScrollToHorizontalOffset(pos.X - 60); // 加点缓冲

                    // 动画：滑入 + 渐显
                    var slide = new DoubleAnimation
                    {
                        From = 100,
                        To = 0,
                        Duration = TimeSpan.FromSeconds(0.4),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    var fade = new DoubleAnimation
                    {
                        To = 1,
                        Duration = TimeSpan.FromSeconds(0.4)
                    };

                    transform.BeginAnimation(TranslateTransform.XProperty, slide);
                    container.BeginAnimation(UIElement.OpacityProperty, fade);
                }

                //await Task.Delay(120); // 控制节奏
            }
        }
        // 启动抽卡式跑马灯动画
        private async Task StartMarqueeScrollAsync(Student winner, int repeatCount = 5)
        {
            double cardWidth = 120;
            double cardMargin = 20;
            double cardSpacing = cardWidth + cardMargin;

            int originalIndex = viewModel.ScrollingStudents
                .Select((s, i) => new { s, i })
                .Where(x => x.s == winner)
                .Select(x => x.i)
                .First();

            // 目标位置为居中显示
            double targetOffset = originalIndex * cardSpacing - scrollingViewer.ActualWidth / 2 + cardWidth / 2;

            double currentOffset = 0;
            double scrollSpeed = 30;
            double slowSpeed = 5;
            double decelerationZone = cardSpacing * 8;

            var tcs = new TaskCompletionSource<bool>();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };

            timer.Tick += (s, e) =>
            {
                currentOffset += scrollSpeed;

                if (targetOffset - currentOffset <= decelerationZone)
                {
                    scrollSpeed = Math.Max(slowSpeed, scrollSpeed * 0.95);
                }

                if (currentOffset >= targetOffset)
                {
                    scrollingViewer.ScrollToHorizontalOffset(targetOffset);
                    timer.Stop();
                    tcs.SetResult(true);
                    return;
                }

                scrollingViewer.ScrollToHorizontalOffset(currentOffset);
            };

            timer.Start();
            await tcs.Task;
        }


        private async Task ShowWinnerFloatingAsync(Student winner)
        {
            int index = viewModel.ScrollingStudents.IndexOf(winner);
            if (index < 0) return;

            scrollingItemsControl.UpdateLayout();
            var container = scrollingItemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
            if (container == null) return;

            // ✅ 从 ContentPresenter 中查找 Border
            var originalBorder = AnimatorService.FindVisualChild<Border>(container);
            if (originalBorder == null) return;

            // 克隆卡片视觉元素
            var clone = new Border
            {
                Width = 120,
                Height = 120,
                CornerRadius = new CornerRadius(20),
                Background = originalBorder.Background,
                Effect = originalBorder.Effect,
                Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
            {
                new TextBlock
                {
                    Text = winner.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
                }
            };

            // 居中定位
            Canvas.SetLeft(clone, (screenWidth - clone.Width) / 2);
            Canvas.SetTop(clone, (screenHeight - clone.Height) / 2);
            overlayCanvas.Children.Add(clone);

            // 设置复合变换
            var transformGroup = new TransformGroup();
            var rotate = new RotateTransform(0);
            var scale = new ScaleTransform(1, 1);
            var translate = new TranslateTransform(0, 0);
            transformGroup.Children.Add(rotate);
            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translate);
            clone.RenderTransform = transformGroup;
            clone.RenderTransformOrigin = new Point(0.5, 0.5);

            // 动画：旋转 + 放大 + 浮起
            var rotateAnim = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            var scaleAnim = new DoubleAnimation(1, 2.2, TimeSpan.FromSeconds(0.5));
            var floatAnim = new DoubleAnimation(0, -40, TimeSpan.FromSeconds(0.5));

            rotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            translate.BeginAnimation(TranslateTransform.YProperty, floatAnim);

            await Task.Delay(1500);

            // 移除浮层
            //overlayCanvas.Children.Remove(clone);
        }


       




    }
}