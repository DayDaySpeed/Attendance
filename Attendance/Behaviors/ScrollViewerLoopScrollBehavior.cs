using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace Attendance.Behaviors
{
    public enum ScrollDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public class ScrollViewerLoopScrollBehavior : Behavior<ScrollViewer>
    {
        private TranslateTransform _transform;
        private double _offset = 0;
        private bool _isPaused = false;
        private bool _isReturning = false;
        private int _currentLoop = 0;

        public double ScrollSpeed { get; set; } = 1.0;
        public ScrollDirection Direction { get; set; } = ScrollDirection.Left;

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(nameof(IsEnabled), typeof(bool), typeof(ScrollViewerLoopScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsLoopingProperty =
            DependencyProperty.Register(nameof(IsLooping), typeof(bool), typeof(ScrollViewerLoopScrollBehavior),
                new PropertyMetadata(false));

        public bool IsLooping
        {
            get => (bool)GetValue(IsLoopingProperty);
            set => SetValue(IsLoopingProperty, value);
        }

        public static readonly DependencyProperty LoopCountProperty =
            DependencyProperty.Register(nameof(LoopCount), typeof(int), typeof(ScrollViewerLoopScrollBehavior),
                new PropertyMetadata(0));

        public int LoopCount
        {
            get => (int)GetValue(LoopCountProperty);
            set => SetValue(LoopCountProperty, value);
        }

        public static readonly DependencyProperty AllowMousePauseProperty =
            DependencyProperty.Register(nameof(AllowMousePause), typeof(bool), typeof(ScrollViewerLoopScrollBehavior),
                new PropertyMetadata(true));

        public bool AllowMousePause
        {
            get => (bool)GetValue(AllowMousePauseProperty);
            set => SetValue(AllowMousePauseProperty, value);
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewerLoopScrollBehavior behavior && behavior.AssociatedObject != null)
            {
                if ((bool)e.NewValue)
                    behavior.StartScroll();
                else
                    behavior.StopScroll();
            }
        }

        protected override async void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseEnter += OnMouseEnter;
            AssociatedObject.MouseLeave += OnMouseLeave;

            if (IsEnabled)
            {
                // 等待数据库和天气加载完成
                await Poems.DatabaseReadyNotifier.ReadySignal.Task;
                await Weather.WeatherReadyNotifier.ReadySignal.Task;

                AssociatedObject.Dispatcher.BeginInvoke(new Action(() =>
                {
                    StartScroll();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            StopScroll();
            AssociatedObject.MouseEnter -= OnMouseEnter;
            AssociatedObject.MouseLeave -= OnMouseLeave;
        }

        private void StartScroll()
        {
            var content = AssociatedObject.Content as FrameworkElement;
            if (content == null) return;

            content.RenderTransformOrigin = new Point(0, 0);

            _transform = content.RenderTransform as TranslateTransform;
            if (_transform == null)
            {
                _transform = new TranslateTransform();
                content.RenderTransform = _transform;
            }

            _offset = 0;
            _isReturning = false;
            _currentLoop = 0;

            CompositionTarget.Rendering += OnRendering;
        }

        private void StopScroll()
        {
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (_isPaused || _transform == null || AssociatedObject == null) return;

            var content = AssociatedObject.Content as FrameworkElement;
            if (content == null) return;

            bool isHorizontal = Direction == ScrollDirection.Left || Direction == ScrollDirection.Right;

            double contentSize = isHorizontal ? content.ActualWidth : content.ActualHeight;
            double viewerSize = isHorizontal ? AssociatedObject.ActualWidth : AssociatedObject.ActualHeight;

            double maxOffset = Direction switch
            {
                ScrollDirection.Left => -contentSize,
                ScrollDirection.Right => viewerSize,
                ScrollDirection.Up => -contentSize,
                ScrollDirection.Down => viewerSize,
                _ => 0
            };

            if (!_isReturning)
            {
                _offset += Direction switch
                {
                    ScrollDirection.Left => -ScrollSpeed,
                    ScrollDirection.Right => ScrollSpeed,
                    ScrollDirection.Up => -ScrollSpeed,
                    ScrollDirection.Down => ScrollSpeed,
                    _ => 0
                };

                if ((Direction == ScrollDirection.Left || Direction == ScrollDirection.Up) && _offset <= maxOffset ||
                    (Direction == ScrollDirection.Right || Direction == ScrollDirection.Down) && _offset >= maxOffset)
                {
                    if (IsLooping)
                    {
                        _currentLoop++;
                        if (LoopCount > 0 && _currentLoop >= LoopCount)
                        {
                            _isReturning = true;
                        }
                        else
                        {
                            _offset = Direction switch
                            {
                                ScrollDirection.Left or ScrollDirection.Up => viewerSize,
                                ScrollDirection.Right or ScrollDirection.Down => -contentSize,
                                _ => 0
                            };
                        }
                    }
                    else
                    {
                        _isReturning = true;
                    }
                }
            }
            else
            {
                _offset += Direction switch
                {
                    ScrollDirection.Left => ScrollSpeed,
                    ScrollDirection.Right => -ScrollSpeed,
                    ScrollDirection.Up => ScrollSpeed,
                    ScrollDirection.Down => -ScrollSpeed,
                    _ => 0
                };

                if ((Direction == ScrollDirection.Left || Direction == ScrollDirection.Up) && _offset >= 0 ||
                    (Direction == ScrollDirection.Right || Direction == ScrollDirection.Down) && _offset <= 0)
                {
                    _offset = 0;
                    ApplyOffset();
                    StopScroll();
                    return;
                }
            }

            ApplyOffset();
        }

        private void ApplyOffset()
        {
            if (_transform == null) return;

            if (Direction == ScrollDirection.Left || Direction == ScrollDirection.Right)
                _transform.X = _offset;
            else
                _transform.Y = _offset;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (AllowMousePause)
                _isPaused = true;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (AllowMousePause)
                _isPaused = false;
        }
    }
}
