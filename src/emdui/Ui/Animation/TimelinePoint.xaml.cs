using System.Windows.Controls;

namespace emdui.Ui.Animation
{
    /// <summary>
    /// Interaction logic for TimelinePoint.xaml
    /// </summary>
    public partial class TimelinePoint : UserControl
    {
        public int Time { get; set; }
        public double Value { get; set; }

        public TimelinePoint()
        {
            InitializeComponent();
        }
    }
}
