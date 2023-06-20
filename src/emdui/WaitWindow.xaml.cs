using System.Threading;
using System.Windows;

namespace emdui
{
    /// <summary>
    /// Interaction logic for WaitWindow.xaml
    /// </summary>
    public partial class WaitWindow : Window
    {
        public WaitWindow()
        {
            InitializeComponent();
        }

        public static bool Wait(string title, string message, CancellationToken ct)
        {
            var window = new WaitWindow();
            window.Owner = MainWindow.Instance;
            window.Icon = MainWindow.Instance.Icon;
            ct.Register(() =>
            {
                window.Dispatcher.Invoke(() => window.Close());
            });
            window.Title = title;
            window.MessageTextBlock.Text = message;
            var result = window.ShowDialog();
            if (ct.IsCancellationRequested)
                return true;
            return false;
        }
    }
}
