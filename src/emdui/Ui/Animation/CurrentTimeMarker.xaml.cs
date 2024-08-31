using System.Windows;
using System.Windows.Controls;

namespace emdui
{
    /// <summary>
    /// Interaction logic for CurrentTimeMarker.xaml
    /// </summary>
    public partial class CurrentTimeMarker : UserControl
    {
        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            nameof(Time),
            typeof(double),
            typeof(CurrentTimeMarker),
            new PropertyMetadata(0.0, (s, e) => ((CurrentTimeMarker)s).Refresh()));

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(CurrentTimeMarker),
            new PropertyMetadata(30.0, (s, e) => ((CurrentTimeMarker)s).Refresh()));

        public double Time
        {
            get => (double)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public CurrentTimeMarker()
        {
            InitializeComponent();
        }

        private void Refresh()
        {
            var margin = Margin;
            margin.Left = Timeline.TimeToX(Scale, Time) - Scale / 2;
            Margin = margin;
        }
    }
}
