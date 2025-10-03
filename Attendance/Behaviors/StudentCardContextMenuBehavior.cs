using Attendance.Classes;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Attendance.Behaviors
{
    //右键添加，修改，删除，选中学生卡片
    public class StudentCardInteractionBehavior : Behavior<Border>
    {
        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(StudentCardInteractionBehavior));

        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(StudentCardInteractionBehavior));

        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(StudentCardInteractionBehavior));

        public static readonly DependencyProperty AddCommandProperty =
            DependencyProperty.Register(nameof(AddCommand), typeof(ICommand), typeof(StudentCardInteractionBehavior));

        public static readonly DependencyProperty SelectedStudentProperty =
            DependencyProperty.Register(nameof(SelectedStudent), typeof(Student), typeof(StudentCardInteractionBehavior), new PropertyMetadata(null, OnSelectedStudentChanged));

        public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.Register(nameof(HighlightBrush), typeof(Brush), typeof(StudentCardInteractionBehavior),
        new PropertyMetadata(Brushes.OrangeRed)); // 默认颜色

        public Brush HighlightBrush
        {
            get => (Brush)GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }


        public ICommand SelectCommand
        {
            get => (ICommand)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }

        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public ICommand EditCommand
        {
            get => (ICommand)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }

        public ICommand AddCommand
        {
            get => (ICommand)GetValue(AddCommandProperty);
            set => SetValue(AddCommandProperty, value);
        }

        public Student SelectedStudent
        {
            get => (Student)GetValue(SelectedStudentProperty);
            set => SetValue(SelectedStudentProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnLeftClick;
            AssociatedObject.MouseRightButtonUp += OnRightClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= OnLeftClick;
            AssociatedObject.MouseRightButtonUp -= OnRightClick;
        }

        private void OnLeftClick(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.DataContext is Student student)
            {
                SelectedStudent = student;
                SelectCommand?.Execute(student);
                UpdateVisualState();
            }
        }

        private void OnRightClick(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.DataContext is not Student student) return;

            var contextMenu = new ContextMenu();

            var addItem = new MenuItem { Header = "➕ 添加学生", Command = AddCommand };
            var editItem = new MenuItem { Header = "✏️ 编辑学生", Command = EditCommand, CommandParameter = student };
            var deleteItem = new MenuItem { Header = "🗑 删除学生", Command = DeleteCommand, CommandParameter = student };

            contextMenu.Items.Add(addItem);
            contextMenu.Items.Add(editItem);
            contextMenu.Items.Add(deleteItem);

            contextMenu.PlacementTarget = AssociatedObject;
            contextMenu.IsOpen = true;
        }

        private static void OnSelectedStudentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is StudentCardInteractionBehavior behavior)
            {
                behavior.UpdateVisualState();
            }
        }

        private void UpdateVisualState()
        {
            if (AssociatedObject.DataContext is Student current)
            {
                if (SelectedStudent == current)
                {
                    AssociatedObject.BorderBrush = HighlightBrush;
                    AssociatedObject.BorderThickness = new Thickness(3);
                }
                else
                {
                    AssociatedObject.BorderBrush = Brushes.Transparent;
                    AssociatedObject.BorderThickness = new Thickness(0);
                }
            }
        }

    }
}
