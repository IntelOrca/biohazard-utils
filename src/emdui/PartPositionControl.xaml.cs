using System;
using System.Windows.Controls;
using System.Windows.Input;
using IntelOrca.Biohazard.Model;

namespace emdui
{
    /// <summary>
    /// Interaction logic for PartPositionControl.xaml
    /// </summary>
    public partial class PartPositionControl : UserControl
    {
        public event EventHandler ValueChanged;

        private Emr.Vector _value;
        private bool _pauseEvents;

        public Emr.Vector Value
        {
            get => _value;
            set
            {
                if (!_value.Equals(value))
                {
                    _value = value;
                    RefreshTextBoxes();
                }
            }
        }

        public PartPositionControl()
        {
            InitializeComponent();
        }

        private void RefreshTextBoxes()
        {
            _pauseEvents = true;
            partXTextBox.Text = _value.x.ToString();
            partYTextBox.Text = _value.y.ToString();
            partZTextBox.Text = _value.z.ToString();
            _pauseEvents = false;
        }

        private void partTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_pauseEvents)
                return;

            ParseTextBoxes();
            RaiseValueChangedEvent();
        }

        private void partTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            ParseTextBoxes();
            RefreshTextBoxes();
            RaiseValueChangedEvent();
        }

        private void ParseTextBoxes()
        {
            _value.x = ParseComponent(partXTextBox.Text);
            _value.y = ParseComponent(partYTextBox.Text);
            _value.z = ParseComponent(partZTextBox.Text);
        }

        private void RaiseValueChangedEvent() => ValueChanged?.Invoke(this, EventArgs.Empty);

        private short ParseComponent(string text)
        {
            short.TryParse(text, out var result);
            return result;
        }

        private void partTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            short delta = 0;
            if (e.Key == Key.Up)
                delta = 1;
            else if (e.Key == Key.Down)
                delta = -1;

            if (delta != 0)
            {
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
                    delta *= 10;

                ParseTextBoxes();

                if (sender == partXTextBox)
                    _value.x += delta;
                else if (sender == partYTextBox)
                    _value.y += delta;
                else if (sender == partZTextBox)
                    _value.z += delta;

                RefreshTextBoxes();
                RaiseValueChangedEvent();
                e.Handled = true;
            }
        }
    }
}
