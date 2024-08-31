using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace emdui
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            nameof(Duration),
            typeof(double),
            typeof(Timeline),
            new PropertyMetadata(0.0));

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(Timeline),
            new PropertyMetadata(30.0, (s, e) => ((Timeline)s).UpdateSeriesScale()));

        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            nameof(Time),
            typeof(double),
            typeof(Timeline),
            new PropertyMetadata(0.0));

        public event EventHandler TimeChanged;
        public event EventHandler PlayToggled;

        private TimelineSeries[] _series = new TimelineSeries[0];

        public Timeline()
        {
            InitializeComponent();
        }

        public double Duration
        {
            get => (double)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public double Time
        {
            get => (double)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public bool Playing { get; set; }

        public TimelineSeries[] Series
        {
            get => _series;
            set
            {
                if (_series != value)
                {
                    _series = value;

                    seriesContainer.Children.Clear();
                    foreach (var series in _series)
                    {
                        seriesContainer.Children.Add(series);
                    }
                }
            }
        }

        private void UpdateSeriesScale()
        {
            foreach (var series in Series)
            {
                series.Scale = Scale;
            }
        }

        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!Playing)
                {
                    var position = e.GetPosition(container);
                    var time = XToTime(Scale, position.X);

                    Seek(time);
                    container.Focus();
                }
            }
        }

        private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var position = e.GetPosition(container);
            var time = XToTime(Scale, position.X);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_series.Any(x => x.TransformingPoint))
                    Seek(time);
            }
        }

        private void Grid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                var diff = (e.Delta / 120.0f) * 2;
                var newScale = Math.Min(50, Math.Max(10, Scale + diff));
                if (newScale != Scale)
                {
                    Scale = newScale;
                }
                e.Handled = true;
            }
        }

        private void container_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    Seek(Time - 1);
                    e.Handled = true;
                    break;
                case Key.Right:
                    Seek(Time + 1);
                    e.Handled = true;
                    break;
                case Key.Space:
                    PlayToggled?.Invoke(this, e);
                    e.Handled = true;
                    break;
            }
        }

        private void container_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (Playing)
                {
                    var position = e.GetPosition(container);
                    var time = XToTime(Scale, position.X);

                    Seek(time);
                    container.Focus();
                }
                e.Handled = true;
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void Seek(double time)
        {
            Time = Math.Max(0, Math.Min(Duration - 1, Math.Round(time)));
            TimeChanged?.Invoke(this, EventArgs.Empty);
        }

        public static double TimeToX(double scale, double time) => (time + 0.5) * scale;
        public static double XToTime(double scale, double x) => (x / scale) - 0.5;
    }
}
