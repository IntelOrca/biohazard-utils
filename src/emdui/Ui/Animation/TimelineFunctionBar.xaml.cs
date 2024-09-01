using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace emdui
{
    /// <summary>
    /// Interaction logic for TimeCodeBar.xaml
    /// </summary>
    public partial class TimelineFunctionBar : UserControl
    {
        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(TimelineFunctionBar),
            new PropertyMetadata(30.0, (s, e) => ((TimelineFunctionBar)s).Refresh()));

        private int[] _flags = new int[0];

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public int[] Flags
        {
            get => _flags;
            set
            {
                _flags = value;
                Refresh();
            }
        }

        public TimelineFunctionBar()
        {
            InitializeComponent();
        }

        private void Refresh()
        {
            canvas.Children.Clear();
            if (_flags == null)
                return;

            for (int t = 0; t < _flags.Length; t++)
            {
                var f = _flags[t];
                if (f == 0)
                    continue;

                CreateFlag(t, f);
            }
        }

        private TextBlock CreateFlag(int time, int flags)
        {
            var left = TimeToX(time) - (Scale / 2);

            var textBlock = new TextBlock();
            textBlock.Foreground = new SolidColorBrush(GetForegroundColor(flags));
            textBlock.Text = $"0x{flags:X}";
            textBlock.FontSize = 6;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.Margin = new Thickness(0, 0, 0, 0);

            var border = new Border();
            border.Background = new SolidColorBrush(GetColor(flags));
            border.BorderBrush = Brushes.Black;
            border.BorderThickness = new Thickness(1, 0, 1, 1);
            Canvas.SetLeft(border, left);
            border.Width = Scale;
            border.Height = Height;
            border.Child = textBlock;
            border.ClipToBounds = true;
            canvas.Children.Add(border);
            return textBlock;
        }

        private static Color GetColor(int function)
        {
            if ((function & 0x10) != 0 || (function & 0x2) != 0)
                return Color.FromRgb(0xFF, 0x66, 0x66);
            return Color.FromRgb(0x66, 0x66, 0xFF);
        }

        private static Color GetForegroundColor(int function)
        {
            return Colors.White;
        }

        private static double Luma(Color c)
        {
            var r2 = 0.2126 * (c.R / 255.0);
            var g2 = 0.7152 * (c.G / 255.0);
            var b2 = 0.0722 * (c.B / 255.0);
            return r2 + g2 + b2;
        }

        private double TimeToX(double time) => Timeline.TimeToX(Scale, time);
    }
}
