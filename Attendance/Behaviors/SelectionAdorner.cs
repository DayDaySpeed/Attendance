using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Attendance.Behaviors
{
    public class SelectionAdorner : Adorner
    {

        //实现一个矩形选择框
        private readonly Rectangle _rectangle;
        public Rect SelectionRect { get; private set; }

        public SelectionAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _rectangle = new Rectangle
            {
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(50, 30, 144, 255))
            };
            AddVisualChild(_rectangle);
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _rectangle;

        protected override Size ArrangeOverride(Size finalSize)
        {
            _rectangle.Arrange(SelectionRect);
            return finalSize;
        }

        public void Update(Rect rect)
        {
            SelectionRect = rect;
            InvalidateArrange();
        }
    }
}
