using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace emdui
{
    /// <summary>
    /// Interaction logic for PathTextBox.xaml
    /// </summary>
    public partial class PathTextBox : UserControl
    {
        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register(nameof(Location), typeof(string), typeof(PathTextBox));

        public string Location
        {
            get => (string)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        public PathTextBox()
        {
            InitializeComponent();
            Location = string.Empty;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Location = dialog.FileName;
            }
        }
    }
}
