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

namespace ZeroMix
{
    public partial class CustomShortcutWindow : Window
    {
        // SOLUSI: Gunakan path yang sama dengan HotkeyCore dari folder AppData.
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

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
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

            // Panggil metode reload di instance HotkeyCore yang sedang berjalan.
            App.HotkeyCoreInstance?.ReloadCustomHotkeys();

            this.DialogResult = true; // Tandai bahwa perubahan disimpan
            // Ubah pesan, karena restart tidak lagi diperlukan
            MessageBox.Show("Pintasan telah diperbarui dan sekarang aktif.", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Tandai bahwa dibatalkan
            this.Close();
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Mencegah tombol diproses oleh TextBox
            e.Handled = true;

            // Dapatkan tombol yang ditekan, abaikan pengubah
            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // Abaikan penekanan hanya pengubah
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

            // Perbarui TextBox dengan nama tombol
            // Gunakan KeyConverter untuk mendapatkan nama yang lebih baik (misal: "OemComma" menjadi ",")
            var keyConverter = new KeyConverter();
            var keyName = keyConverter.ConvertToString(key);
            if (keyName != null)
                hotkeyParts.Add(keyName);

            (sender as TextBox)!.Text = string.Join("+", hotkeyParts);
        }

        private void AddShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            string hotkey = HotkeyTextBox.Text;
            // BUG FIX: Gunakan SelectedValue (path) jika ada, jika tidak, gunakan Text.
            // Ini memastikan path file yang disimpan, bukan hanya nama aplikasinya.
            string appPath = AppPathComboBox.SelectedValue as string ?? AppPathComboBox.Text;

            if (string.IsNullOrWhiteSpace(hotkey) || hotkey == "Klik dan tekan kombinasi tombol")
            {
                MessageBox.Show("Silakan rekam hotkey.", "Informasi Hilang", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(appPath))
            {
                MessageBox.Show("Silakan pilih jalur aplikasi.", "Informasi Hilang", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Cek duplikat hotkey
            if (Shortcuts.Any(s => s.Hotkey.Equals(hotkey, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Hotkey ini sudah digunakan. Silakan pilih yang lain.", "Hotkey Duplikat", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Shortcuts.Add(new CustomShortcut { Hotkey = hotkey, ApplicationPath = appPath });

            // Kosongkan input untuk entri berikutnya
            HotkeyTextBox.Text = "Klik dan tekan kombinasi tombol";
            AppPathComboBox.Text = "";
            AppPathComboBox.SelectedIndex = -1;
        }

        private void DeleteShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedShortcut = ShortcutListView.SelectedItem as CustomShortcut;
            if (selectedShortcut == null)
            {
                MessageBox.Show("Silakan pilih pintasan dari daftar untuk dihapus.", "Tidak Ada Pintasan Dipilih", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Apakah Anda yakin ingin menghapus pintasan '{selectedShortcut.Hotkey}'?", "Konfirmasi Penghapusan", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                Shortcuts.Remove(selectedShortcut);
            }
        }

        private void LoadInstalledApplications()
        {
            var appList = new List<InstalledApplication>();
            // Tambahkan Desktop ke daftar path yang akan dipindai
            string[] scanPaths =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) // <-- Path Desktop ditambahkan di sini
            };

            foreach (var path in scanPaths.Where(Directory.Exists))
            {
                // Untuk Desktop, kita hanya pindai folder utama, bukan sub-folder.
                var searchOption = path.Contains("Desktop") ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
                var lnkFiles = Directory.GetFiles(path, "*.lnk", searchOption);
                foreach (var file in lnkFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (!string.IsNullOrEmpty(name) && !appList.Any(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                        appList.Add(new InstalledApplication { Name = name, Path = file });
                }
            }

            // Urutkan berdasarkan nama dan tambahkan ke ObservableCollection
            foreach (var app in appList.OrderBy(a => a.Name))
            {
                InstalledApps.Add(app);
            }
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

    public class InstalledApplication
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
