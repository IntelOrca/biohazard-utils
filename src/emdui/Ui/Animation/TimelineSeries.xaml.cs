using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using emdui.Ui.Animation;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TimelineSeries.xaml
    /// </summary>
    public partial class TimelineSeries : UserControl
    {
        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(TimelineSeries),
            new PropertyMetadata(30.0, (s, e) => ((TimelineSeries)s).Refresh()));

        private Polyline _line;
        private TimelinePoint[] _pointControls = new TimelinePoint[0];
        private double[] _points = new double[0];
        private TimelinePoint _grabbedPoint;
        private Vector _grabDelta;

        public event EventHandler<PointChangedEventArgs> PointChanged;

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public object Points
        {
            get => _points;
            set
            {
                if (_points != value)
                {
                    _points = (double[])value;
                    Refresh();
                }
            }
        }

        public bool TransformingPoint => _grabbedPoint != null;

        public TimelineSeries()
        {
            InitializeComponent();
            SizeChanged += (s, e) => Refresh();
        }

        private void Refresh()
        {
            var removeCount = _pointControls.Length - _points.Length;
            for (var i = 0; i < removeCount; i++)
            {
                var p = _pointControls[_pointControls.Length - i - 1];
                canvas.Children.Remove(p);
            }
            Array.Resize(ref _pointControls, _points.Length);

            for (var t = 0; t < _points.Length; t++)
            {
                var value = _points[t];
                var x = TimeToX(t);
                var y = ValueToY(value);

                var point = _pointControls[t];
                if (point == null)
                {
                    point = new TimelinePoint();
                    _pointControls[t] = point;
                    canvas.Children.Add(point);
                }

                point.Time = t;
                point.Value = value;

                Canvas.SetLeft(point, x - (point.Width / 2));
                Canvas.SetTop(point, y - (point.Height / 2));
                point.Width = Scale * 0.5;
                point.Height = point.Width;
            }

            if (_pointControls.Length >= 2)
            {
                if (_line == null)
                {
                    _line = new Polyline();
                    _line.IsHitTestVisible = false;
                    _line.Stroke = Brushes.DarkGray;
                    _line.StrokeThickness = 1;
                    canvas.Children.Insert(0, _line);
                }

                _line.Points.Clear();
                for (var t = 0; t < _points.Length; t++)
                {
                    var x = TimeToX(t);
                    var y = ValueToY(_points[t]);
                    _line.Points.Add(new Point(x, y));
                }
            }
            else
            {
                if (_line != null)
                {
                    canvas.Children.Remove(_line);
                    _line = null;
                }
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var pp = VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(e.OriginalSource as DependencyObject)));
                if (pp is TimelinePoint p && _pointControls.Contains(p))
                {
                    var pointPosition = new Point(
                        Canvas.GetLeft(p),
                        Canvas.GetTop(p));

                    _grabbedPoint = p;
                    _grabDelta = pointPosition - e.GetPosition(this);
                    e.Handled = true;
                }
            }
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (_grabbedPoint != null)
            {
                var time = _grabbedPoint.Time;
                var newPosition = e.GetPosition(this) + _grabDelta;
                var newPoint = newPosition +
                    new Vector(_grabbedPoint.Width / 2, _grabbedPoint.Height / 2);
                var oldValue = _points[time];
                var newValue = YToValue(newPoint.Y);
                _grabbedPoint.Value = newValue;
                _points[time] = newValue;
                Refresh();
                PointChanged?.Invoke(
                    this,
                    new PointChangedEventArgs(time, oldValue, newValue));
            }
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            _grabbedPoint = null;
        }

        private double TimeToX(double time) => Timeline.TimeToX(Scale, time);

        private double ValueToY(double v)
        {
            var top = 32;
            var bottom = ActualHeight - 32;
            var y = Utility.Lerp(top, bottom, (-v + 1) / 2);
            return y;
        }

        private double YToValue(double y)
        {
            var top = 32;
            var bottom = ActualHeight - 32;
            var a = (y - top) / (bottom - top);
            var b = Utility.Lerp(1, -1, a);
            var c = Math.Min(1, Math.Max(-1, b));
            return c;
        }
    }

    public class PointChangedEventArgs : EventArgs
    {
        public int Time { get; }
        public double OldValue { get; }
        public double NewValue { get; }

        public PointChangedEventArgs(int time, double oldValue, double newValue)
        {
            Time = time;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
