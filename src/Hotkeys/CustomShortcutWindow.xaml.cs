using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Runtime.InteropServices;

namespace ZeroMix.Hotkeys
{
    public partial class CustomShortcutWindow : Window
    {
        #region Window Blur Effect
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_BLURBEHIND = 3, // Standard blur effect
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        internal void EnableBlur()
        {
            var windowHelper = new System.Windows.Interop.WindowInteropHelper(this);
            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData { Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY, SizeOfData = accentStructSize, Data = accentPtr };
            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
        #endregion

        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ZeroMix");
        private static readonly string ShortcutsFilePath = Path.Combine(AppDataFolder, "custom_shortcuts.json");

        public ObservableCollection<CustomShortcut> Shortcuts { get; set; }
        public ObservableCollection<InstalledApplication> InstalledApps { get; set; }

        public CustomShortcutWindow()
        {
            InitializeComponent();
            Shortcuts = new ObservableCollection<CustomShortcut>();
            InstalledApps = new ObservableCollection<InstalledApplication>();
            ShortcutListView.ItemsSource = Shortcuts;
            AppPathComboBox.ItemsSource = InstalledApps;
            LoadShortcuts();
            LoadInstalledApplications();

            // Pastikan folder ada sebelum mencoba menyimpan
            Directory.CreateDirectory(AppDataFolder);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Mengaktifkan efek glassmorphism agar seragam dengan MainWindow
            EnableBlur();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Pilih Aplikasi"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AppPathComboBox.Text = openFileDialog.FileName;
            }
        }

        private void AppPathComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (AppPathComboBox.SelectedItem is InstalledApplication selectedApp)
            {
                AppPathComboBox.Text = selectedApp.Name;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveShortcuts();
            App.HotkeyCoreInstance?.ReloadCustomHotkeys();
            System.Windows.MessageBox.Show("Pintasan telah diperbarui dan sekarang aktif.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

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

            var keyConverter = new KeyConverter();
            var keyName = keyConverter.ConvertToString(key);
            if (keyName != null)
                hotkeyParts.Add(keyName);

            (sender as System.Windows.Controls.TextBox)!.Text = string.Join("+", hotkeyParts);
        }

        private void AddShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            string hotkey = HotkeyTextBox.Text;
            string appPath = AppPathComboBox.SelectedValue as string ?? AppPathComboBox.Text;

            if (string.IsNullOrWhiteSpace(hotkey) || hotkey == "Click and press key combination")
            {
                System.Windows.MessageBox.Show("Silakan rekam hotkey.", "Informasi Hilang", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(appPath))
            {
                System.Windows.MessageBox.Show("Silakan pilih jalur aplikasi.", "Informasi Hilang", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Shortcuts.Any(s => s.Hotkey.Equals(hotkey, StringComparison.OrdinalIgnoreCase)))
            {
                System.Windows.MessageBox.Show("Hotkey ini sudah digunakan.", "Hotkey Duplikat", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Shortcuts.Add(new CustomShortcut { Hotkey = hotkey, ApplicationPath = appPath });
            HotkeyTextBox.Text = "Click and press key combination";
            AppPathComboBox.Text = "";
            AppPathComboBox.SelectedIndex = -1;
        }

        private void DeleteShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedShortcut = ShortcutListView.SelectedItem as CustomShortcut;
            if (selectedShortcut == null)
            {
                System.Windows.MessageBox.Show("Silakan pilih pintasan.", "Tidak Ada Pintasan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (System.Windows.MessageBox.Show($"Hapus '{selectedShortcut.Hotkey}'?", "Konfirmasi", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Shortcuts.Remove(selectedShortcut);
            }
        }

        private void LoadInstalledApplications()
        {
            var appList = new List<InstalledApplication>();
            string[] scanPaths =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            foreach (var path in scanPaths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        var searchOption = path.Contains("Desktop") ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
                        var lnkFiles = Directory.GetFiles(path, "*.lnk", searchOption);
                        foreach (var file in lnkFiles)
                        {
                            string name = Path.GetFileNameWithoutExtension(file);
                            if (!string.IsNullOrEmpty(name) && !appList.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                                appList.Add(new InstalledApplication { Name = name, Path = file });
                        }
                    }
                }
                catch { }
            }

            foreach (var app in appList.OrderBy(a => a.Name))
            {
                InstalledApps.Add(app);
            }
        }

        private void LoadShortcuts()
        {
            if (File.Exists(ShortcutsFilePath))
            {
                try
                {
                    var json = File.ReadAllText(ShortcutsFilePath);
                    var shortcuts = JsonConvert.DeserializeObject<ObservableCollection<CustomShortcut>>(json);
                    if (shortcuts != null)
                    {
                        Shortcuts.Clear();
                        foreach(var s in shortcuts) Shortcuts.Add(s);
                    }
                }
                catch { }
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
        
        // Untuk tampilan di tabel — nama file saja
        public string ApplicationName => string.IsNullOrEmpty(ApplicationPath) 
            ? "" 
            : Path.GetFileNameWithoutExtension(ApplicationPath);
    }

    public class InstalledApplication
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
