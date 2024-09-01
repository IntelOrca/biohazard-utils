using System;
using System.Windows;

namespace emdui
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        private Func<string, bool> _validate;
        private string _result;

        public InputWindow()
        {
            InitializeComponent();
        }

        public static string Show(string title, string description, string defaultValue, Func<string, bool> validate = null)
        {
            var window = new InputWindow();
            window.Owner = MainWindow.Instance;
            window._validate = validate;
            window.Title = title;
            window.descriptionLabel.Text = description;
            window.inputTextBox.Text = defaultValue;
            window.inputTextBox.Focus();
            window.inputTextBox.SelectAll();
            if (window.ShowDialog() == true)
            {
                return window._result;
            }
            return null;
        }

        private void inputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_validate != null)
            {
                okButton.IsEnabled = _validate(inputTextBox.Text);
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            _result = inputTextBox.Text;
            DialogResult = true;
            Close();
        }
    }
}
