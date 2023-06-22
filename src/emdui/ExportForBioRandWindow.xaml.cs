using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace emdui
{
    /// <summary>
    /// Interaction logic for ExportForBioRandWindow.xaml
    /// </summary>
    public partial class ExportForBioRandWindow : Window
    {
        public Project Project { get; set; }

        public ExportForBioRandWindow()
        {
            InitializeComponent();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var location = locationTextBox.Location.Trim();
            if (string.IsNullOrEmpty(location))
            {
                MessageBox.Show(this, "Location not specified", "Invalid location", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var characterName = ValidateCharacterName(characterTextBox.Text, out var errorMessage);
            if (characterName == null)
            {
                MessageBox.Show(this, errorMessage, "Invalid character name", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var templateCharacter = (templateComboBox.SelectedItem as ComboBoxItem).Content.ToString().ToLowerInvariant();

            try
            {
                ExportToBioRand(templateCharacter, characterName, location);
                MessageBox.Show(this, "Character was successfully exported", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(location);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string ValidateCharacterName(string characterName, out string errorMessage)
        {
            if (characterName.Length == 0)
            {
                errorMessage = "Character name not specified.";
                return null;
            }

            var regex = new Regex("^[a-z0-9._]+$");
            if (!regex.IsMatch(characterName))
            {
                errorMessage = "Character name must only contain alphanumeric characters, underscore, and fullstop.";
                return null;
            }

            errorMessage = null;
            return characterName.ToLowerInvariant();
        }

        private void ExportToBioRand(string templateCharacter, string characterName, string destination)
        {
            var exporter = new BioRandExporter();
            exporter.Import(Project);
            exporter.Export(templateCharacter, characterName, destination);
        }
    }
}
