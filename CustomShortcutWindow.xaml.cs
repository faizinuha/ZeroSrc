using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ZeroMix
{
    public partial class CustomShortcutWindow : Window
    {
        private const string ShortcutsFilePath = "custom_shortcuts.json";
        public ObservableCollection<CustomShortcut> Shortcuts { get; set; }

        public CustomShortcutWindow()
        {
            InitializeComponent();
            Shortcuts = new ObservableCollection<CustomShortcut>();
            ShortcutListView.ItemsSource = Shortcuts;
            LoadShortcuts();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select an Application"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AppPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveShortcuts();
            this.Close();
            MessageBox.Show("Shortcuts saved. Please restart the application for the new hotkeys to take effect.", "Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Prevent the key from being processed by the TextBox
            e.Handled = true;

            // Get the pressed key, ignoring modifiers
            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // Ignore modifier-only presses
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin)
            {
                return;
            }

            var hotkeyParts = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) hotkeyParts.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) hotkeyParts.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) hotkeyParts.Add("Shift");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) hotkeyParts.Add("Win");

            // Update the TextBox with the key name
            hotkeyParts.Add(key.ToString());

            (sender as TextBox)!.Text = string.Join("+", hotkeyParts);
        }

        private void AddShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            string hotkey = HotkeyTextBox.Text;
            string appPath = AppPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(hotkey) || hotkey == "Click here and press a key combination")
            {
                MessageBox.Show("Please record a hotkey.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(appPath))
            {
                MessageBox.Show("Please select an application path.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Shortcuts.Add(new CustomShortcut { Hotkey = hotkey, ApplicationPath = appPath });

            // Clear inputs for next entry
            HotkeyTextBox.Text = "Click here and press a key combination";
            AppPathTextBox.Clear();
        }

        private void LoadShortcuts()
        {
            if (File.Exists(ShortcutsFilePath))
            {
                var json = File.ReadAllText(ShortcutsFilePath);
                var shortcuts = JsonConvert.DeserializeObject<ObservableCollection<CustomShortcut>>(json);
                if (shortcuts != null)
                {
                    Shortcuts = shortcuts;
                    ShortcutListView.ItemsSource = Shortcuts;
                }
            }
        }

        private void SaveShortcuts()
        {
            var json = JsonConvert.SerializeObject(Shortcuts, Formatting.Indented);
            File.WriteAllText(ShortcutsFilePath, json);
        }
    }

    public class CustomShortcut
    {
        public string Hotkey { get; set; } = "";
        public string ApplicationPath { get; set; } = "";
    }
}