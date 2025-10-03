using Attendance.Classes;
using Attendance.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Attendance.Animation
{
    public class TripleRollCallAnimator
    {
        private readonly RollCallViewModel viewModel;
        private readonly ScrollViewer topLeftScrollViewer, topRightScrollViewer, bottomScrollViewer, originalscrollViewer;
        private readonly ItemsControl topLeftItemsControl, topRightItemsControl, bottomItemsControl, originalitemsControl;
        private readonly FrameworkElement settingsPanel;
        private readonly double screenWidth, screenHeight;
        private readonly Canvas overlayCanvas;

        public TripleRollCallAnimator(
            RollCallViewModel viewModel,
            ScrollViewer originalscrollViewer,
            ScrollViewer topLeftScrollViewer,
            ScrollViewer topRightScrollViewer,
            ScrollViewer bottomScrollViewer,
            ItemsControl originalitemsControl,
            ItemsControl topLeftItemsControl,
            ItemsControl topRightItemsControl,
            ItemsControl bottomItemsControl,
            FrameworkElement settingsPanel,
            Canvas overlayCanvas)
        {
            this.viewModel = viewModel;
            this.topLeftScrollViewer = topLeftScrollViewer;
            this.topRightScrollViewer = topRightScrollViewer;
            this.bottomScrollViewer = bottomScrollViewer;
            this.topLeftItemsControl = topLeftItemsControl;
            this.topRightItemsControl = topRightItemsControl;
            this.bottomItemsControl = bottomItemsControl;
            this.settingsPanel = settingsPanel;
            this.originalscrollViewer = originalscrollViewer;
            this.originalitemsControl = originalitemsControl;
            screenWidth = SystemParameters.PrimaryScreenWidth;
            screenHeight = SystemParameters.PrimaryScreenHeight;
            this.overlayCanvas = overlayCanvas;
        }
        public async Task StartAsync()
        {
            // ✅ 淡出设置区域
            await FadeOutSettingsPanel();

            // ✅ 扩展滚动区域
            await ExpandScrollViewer();

            // ✅ 淡出旧元素
            await FadeOutOldItems();


            await Task.WhenAll(
                PlayAddAnimationAsync(topLeftItemsControl, topLeftScrollViewer, viewModel.TopLeftStudents, viewModel.DisplayedStudents),
                PlayAddAnimationAsync(topRightItemsControl, topRightScrollViewer, viewModel.TopRightStudents, viewModel.DisplayedStudents),
                PlayAddAnimationAsync(bottomItemsControl, bottomScrollViewer, viewModel.BottomStudents, viewModel.DisplayedStudents)
            );

            // ✅ 随机抽取三人（从已有集合中）
            var selcted = AnimatorService.DrawStudentsWithSettings(viewModel.DisplayedStudents, 3, viewModel.SelectedGenderPreference, viewModel.SelectedTailDigit);
            var leftWinner = selcted[0];
            var rightWinner = selcted[1];
            var bottomWinner = selcted[2];

            if (leftWinner == null || rightWinner == null || bottomWinner == null)
                return;

            // ✅ 三人滚动定位
            await Task.WhenAll(
                ScrollToStudentAnimatedAsync(leftWinner, topLeftScrollViewer, topLeftItemsControl, viewModel.TopLeftStudents),
                ScrollToStudentAnimatedAsync(rightWinner, topRightScrollViewer, topRightItemsControl, viewModel.TopRightStudents),
                ScrollToStudentAnimatedAsync(bottomWinner, bottomScrollViewer, bottomItemsControl, viewModel.BottomStudents)
            );


            // ✅ 扇形展开展示
            await ShowWinnersFanOutAsync(leftWinner, bottomWinner, rightWinner);
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


        //添加
        private async Task PlayAddAnimationAsync(ItemsControl itemsControl, ScrollViewer scrollViewer, ObservableCollection<Student> targetCollection, IEnumerable<Student> sourceStudents)
        {
            scrollViewer.Visibility = Visibility.Visible;
            targetCollection.Clear();

            foreach (var student in sourceStudents)
            {
                targetCollection.Add(student);
                await Task.Delay(30); // 节奏控制
                itemsControl.UpdateLayout();
                scrollViewer.UpdateLayout();

                var index = targetCollection.IndexOf(student);
                var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                if (container != null)
                {
                    container.Opacity = 1;

                    var pos = container.TransformToAncestor(scrollViewer).Transform(new Point(0, 0));
                    scrollViewer.ScrollToHorizontalOffset(pos.X - 60);
                }
            }
        }

        //滚动方法
        private async Task ScrollToStudentAnimatedAsync(Student targetStudent, ScrollViewer scrollViewer, ItemsControl itemsControl, ObservableCollection<Student> students)
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

            // 创建动画：先快后慢，时间更长
            var animation = new DoubleAnimation
            {
                From = scrollViewer.HorizontalOffset,
                To = targetOffset,
                Duration = TimeSpan.FromSeconds(6), // 更长时间
                EasingFunction = new ExponentialEase
                {
                    EasingMode = EasingMode.EaseOut,
                    Exponent = 4 // 越大越快起慢收
                }
            };

            // 使用 AnimationClock 控制滚动
            var clock = animation.CreateClock();
            clock.CurrentTimeInvalidated += (s, e) =>
            {
                if (clock.CurrentProgress.HasValue)
                {
                    double value = animation.From.Value + (animation.To.Value - animation.From.Value) * clock.CurrentProgress.Value;
                    scrollViewer.ScrollToHorizontalOffset(value);
                }
            };

            clock.Completed += (s, e) =>
            {
                scrollViewer.ScrollToHorizontalOffset(targetOffset); // 最终锁定
            };

            clock.Controller.Begin();

            await Task.Delay(animation.Duration.TimeSpan + TimeSpan.FromMilliseconds(200));
        }



        //扇形展开浮层动画
        public async Task ShowWinnersFanOutAsync(Student leftWinner, Student topWinner, Student rightWinner)
        {
            var winners = new[] { leftWinner, topWinner, rightWinner };
            var directions = new[]
            {
                new Vector(-150, 0),   // 左
                new Vector(0, -150),   // 上
                new Vector(150, 0)     // 右
            };

            var itemsControls = new[] { topLeftItemsControl, bottomItemsControl, topRightItemsControl };

            for (int i = 0; i < winners.Length; i++)
            {
                var winner = winners[i];
                var itemsControl = itemsControls[i];

                int index = itemsControl.Items.IndexOf(winner);
                if (index < 0) continue;

                itemsControl.UpdateLayout();
                FrameworkElement container = null;
                int retry = 0;
                while (container == null && retry < 20)
                {
                    itemsControl.UpdateLayout();
                    container = itemsControl.ItemContainerGenerator.ContainerFromIndex(index) as FrameworkElement;
                    await Task.Delay(50);
                    retry++;
                }
                if (container == null) continue;

                var originalBorder = AnimatorService.FindVisualChild<Border>(container);
                if (originalBorder == null) continue;
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
                    },
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };

                Canvas.SetLeft(clone, (screenWidth - clone.Width) / 2);
                Canvas.SetTop(clone, (screenHeight - clone.Height) / 2);
                overlayCanvas.Children.Add(clone);

                var transformGroup = new TransformGroup();
                var translate = new TranslateTransform(0, 0);
                var rotate = new RotateTransform(0);
                var scale = new ScaleTransform(1, 1);
                transformGroup.Children.Add(scale);
                transformGroup.Children.Add(rotate);
                transformGroup.Children.Add(translate);
                clone.RenderTransform = transformGroup;

                var dir = directions[i];

                var moveAnimX = new DoubleAnimation(0, dir.X, TimeSpan.FromSeconds(0.5))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var moveAnimY = new DoubleAnimation(0, dir.Y, TimeSpan.FromSeconds(0.5))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                var rotateAnim = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.8))
                {
                    EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
                };
                var scaleAnim = new DoubleAnimation(1, 1.8, TimeSpan.FromSeconds(0.5));

                translate.BeginAnimation(TranslateTransform.XProperty, moveAnimX);
                translate.BeginAnimation(TranslateTransform.YProperty, moveAnimY);
                rotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

                await Task.Delay(300); // 控制节奏
            }

            await Task.Delay(1500); // 展示停留时间

            // 可选：清除浮层
            //overlayCanvas.Children.Clear();
        }
    }
}


