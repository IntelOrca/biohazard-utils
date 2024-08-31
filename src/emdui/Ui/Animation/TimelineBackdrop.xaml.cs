using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TimelineBackdrop.xaml
    /// </summary>
    public partial class TimelineBackdrop : UserControl
    {
        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(TimelineBackdrop),
            new PropertyMetadata(30.0, (s, e) => ((TimelineBackdrop)s).Refresh()));

        private readonly List<Line> _verticalLines = new List<Line>();
        private Line[] _horizontalLines = new Line[3];

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public TimelineBackdrop()
        {
            InitializeComponent();

            SizeChanged += TimeCodeBar_SizeChanged;
        }

        private void TimeCodeBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Refresh(e.NewSize);
        }

        private void Refresh()
        {
            Refresh(new Size(ActualWidth, ActualHeight));
        }

        private void Refresh(Size size)
        {
            if (double.IsNaN(size.Width) || double.IsNaN(size.Height))
                return;

            UpdateHorizontalLines(size);
            UpdateVerticalLines(size);
        }

        private void UpdateHorizontalLines(Size size)
        {
            var values = new double[] { 1, 0, -1 };
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                var hLine = _horizontalLines[i];
                if (hLine == null)
                {
                    hLine = new Line();
                    hLine.IsHitTestVisible = false;
                    hLine.Stroke = Brushes.LightGray;
                    _horizontalLines[i] = hLine;
                    canvas.Children.Add(hLine);
                }
                hLine.X1 = TimeToX(-0.25f);
                hLine.Y1 = ValueToY(size.Height, value);
                hLine.X2 = size.Width;
                hLine.Y2 = hLine.Y1;
                _horizontalLines[i] = hLine;
            }
        }

        private void UpdateVerticalLines(Size size)
        {
            var i = 0;
            while (i < 1024)
            {
                var line = CreateLine(i, size.Height);
                if (line.X1 >= size.Width)
                    break;

                i++;
            }
        }

        private Line CreateLine(int time, double height)
        {
            while (time >= _verticalLines.Count)
            {
                _verticalLines.Add(null);
            }

            var line = _verticalLines[time];
            if (line == null)
            {
                line = new Line();
                line.Stroke = Brushes.LightGray;
                canvas.Children.Add(line);
                _verticalLines[time] = line;
            }

            line.X1 = TimeToX(time);
            line.Y1 = 0;
            line.X2 = line.X1;
            line.Y2 = height;

            return line;
        }

        private double TimeToX(double time) => Timeline.TimeToX(Scale, time);

        private double ValueToY(double height, double v)
        {
            var top = 32;
            var bottom = height - 32;
            var y = Utility.Lerp(top, bottom, (-v + 1) / 2);
            return y;
        }
    }
}
