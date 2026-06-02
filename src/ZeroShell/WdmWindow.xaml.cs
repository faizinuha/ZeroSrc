using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;

using Brushes         = System.Windows.Media.Brushes;
using Color           = System.Windows.Media.Color;
using ColorConverter  = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Orientation     = System.Windows.Controls.Orientation;
using ComboBox        = System.Windows.Controls.ComboBox;
using TextBox         = System.Windows.Controls.TextBox;
using Button          = System.Windows.Controls.Button;
using Cursors         = System.Windows.Input.Cursors;
using IOPath          = System.IO.Path;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ZeroMix.ZeroShell
{
    public partial class WdmWindow : Window
    {
        private WdmState _state = new();
        private string _selectedCategory = "Taskbar";
        private readonly string _statePath =
            IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZeroShell", "wdm.json");

        // Removed StartMenu — Win10 UWP process cannot be styled externally
        private readonly string[] _categories =
            { "Taskbar", "Notification", "Explorer", "Desktop", "Layouts" };

        private const string C_TEXT_PRI   = "#F0F0F0";
        private const string C_TEXT_SEC   = "#9D9D9D";
        private const string C_TEXT_MUTED = "#5A5A5A";
        private const string C_BORDER     = "#3F3F46";
        private const string C_ACCENT     = "#0078D4";
        private const string C_GREEN      = "#4EC94E";
        private const string C_GREY_DOT   = "#5A5A5A";

        public WdmWindow()
        {
            InitializeComponent();
            LoadState();
            BuildSidebar();
            SelectCategory("Taskbar");
            UpdateStatusBar();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

        #region Sidebar
        private void BuildSidebar()
        {
            CategoryList.Items.Clear();
            foreach (var cat in _categories)
            {
                bool isActive = IsCategoryActive(cat);
                var item = new ListBoxItem { Tag = cat, Content = BuildSidebarContent(cat, isActive) };
                CategoryList.Items.Add(item);
            }
            foreach (ListBoxItem item in CategoryList.Items)
                if (item.Tag?.ToString() == _selectedCategory) { CategoryList.SelectedItem = item; break; }
        }

        private bool IsCategoryActive(string cat)
        {
            if (cat == "Layouts" || cat == "Desktop") return false;
            if (cat == "Notification") return _state.Entries.ContainsKey("__Notification__");
            return WdmCategories.ClassMap.ContainsKey(cat) &&
                   WdmCategories.ClassMap[cat].Any(cls => _state.Entries.ContainsKey(cls));
        }

        private StackPanel BuildSidebarContent(string cat, bool isActive)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new Ellipse
            {
                Width = 7, Height = 7,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? C_GREEN : C_GREY_DOT)),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            string icon = cat switch
            {
                "Taskbar"      => "▬",
                "Notification" => "🔔",
                "Explorer"     => "📁",
                "Desktop"      => "🖥",
                "Layouts"      => "◈",
                _              => "•"
            };
            sp.Children.Add(new TextBlock
            {
                Text = icon, FontSize = 12,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? C_ACCENT : C_TEXT_SEC))
            });
            sp.Children.Add(new TextBlock
            {
                Text = WdmCategories.DisplayNames.TryGetValue(cat, out var dn) ? dn : cat,
                FontSize = 13, VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? C_TEXT_PRI : "#C8C8C8"))
            });

            if (isActive)
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A0078D4")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#330078D4")),
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 1, 5, 1), Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = "ON", FontSize = 9, FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_ACCENT))
                };
                sp.Children.Add(badge);
            }
            return sp;
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryList.SelectedItem is ListBoxItem item && item.Tag is string cat)
                SelectCategory(cat);
        }
        #endregion

        #region Category Panels
        private void SelectCategory(string cat)
        {
            _selectedCategory = cat;
            // Clear the ContentControl so MVVM DataTemplate can be applied fresh
            if (CategoryContent != null) CategoryContent.Content = null;
            switch (cat)
            {
                case "Layouts": BuildLayoutsPanelMVVM(); return;
                case "Desktop": BuildDesktopPanelMVVM(); return;
                default:        BuildStylePanelMVVM(cat); return;
            }
        }

        private void BuildStylePanel(string cat)
        {
            var ContentPanel = new StackPanel();

            WdmEntry? existing = null;
            if (cat == "Notification") _state.Entries.TryGetValue("__Notification__", out existing);
            else
            {
                var classes = WdmCategories.ClassMap.TryGetValue(cat, out var cls) ? cls : Array.Empty<string>();
                existing = classes.Select(c => _state.Entries.TryGetValue(c, out var e) ? e : null)
                                  .FirstOrDefault(e => e != null);
            }
            existing ??= new WdmEntry();

            string displayName = WdmCategories.DisplayNames.TryGetValue(cat, out var dn) ? dn : cat;
            ContentPanel.Children.Add(MakeHeader(displayName, GetCategoryDesc(cat)));
            ContentPanel.Children.Add(MakeDivider());

            ContentPanel.Children.Add(MakeSectionLabel("STYLE"));
            var styleCombo = new ComboBox { Margin = new Thickness(0, 6, 0, 0) };
            styleCombo.Items.Add("Acrylic Dark");
            styleCombo.Items.Add("Acrylic Light");
            styleCombo.Items.Add("Blur Only");
            styleCombo.Items.Add("Glass Clear");
            styleCombo.Items.Add("Full Transparent");
            styleCombo.Items.Add("Floating macOS");
            styleCombo.SelectedIndex = Math.Max(0, (int)existing.Style - 1);
            ContentPanel.Children.Add(styleCombo);

            ContentPanel.Children.Add(MakeSectionLabel($"OPACITY  —  {(int)(existing.Alpha / 2.55)}%", new Thickness(0, 16, 0, 0)));
            var opSlider = new Slider { Minimum = 0, Maximum = 255, Value = existing.Alpha, Margin = new Thickness(0, 6, 0, 0) };
            opSlider.ValueChanged += (s, ev) =>
            {
                var lbl = ContentPanel.Children.OfType<TextBlock>().FirstOrDefault(t => t.Text.StartsWith("OPACITY"));
                if (lbl != null) lbl.Text = $"OPACITY  —  {(int)(ev.NewValue / 2.55)}%";
            };
            ContentPanel.Children.Add(opSlider);

            ContentPanel.Children.Add(MakeSectionLabel("COLOR TINT (HEX)", new Thickness(0, 16, 0, 0)));
            ContentPanel.Children.Add(new TextBox { Text = existing.ColorHex, Margin = new Thickness(0, 6, 0, 0) });

            if (IsCategoryActive(cat))
            {
                ContentPanel.Children.Add(MakeDivider(new Thickness(0, 20, 0, 0)));
                var infoCard = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A4EC94E")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334EC94E")),
                    BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 8, 0, 0)
                };
                infoCard.Child = new TextBlock
                {
                    Text = $"● Active — {existing.Style}  |  Opacity {(int)(existing.Alpha / 2.55)}%",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_GREEN)),
                    FontSize = 11
                };
                ContentPanel.Children.Add(infoCard);
            }

            CategoryContent.Content = ContentPanel;
        }

        private string GetCategoryDesc(string cat) => cat switch
        {
            "Taskbar"      => "Style the Windows taskbar",
            "Notification" => "Style the notification & action center panel",
            "Explorer"     => "Style File Explorer windows",
            _              => ""
        };

        private void BuildDesktopPanel()
        {
            var ContentPanel = new StackPanel();
            ContentPanel.Children.Add(MakeHeader("Desktop", "Manage desktop icons"));
            ContentPanel.Children.Add(MakeDivider());
            ContentPanel.Children.Add(MakeSectionLabel("DESKTOP ICONS"));

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var hideBtn = MakeCardButton("Hide Icons", "🙈");
            hideBtn.MouseLeftButtonDown += (s, e) => ShellHelper.HideDesktopIcons();
            row.Children.Add(hideBtn);
            var showBtn = MakeCardButton("Show Icons", "👁");
            showBtn.MouseLeftButtonDown += (s, e) => ShellHelper.ShowDesktopIcons();
            showBtn.Margin = new Thickness(10, 0, 0, 0);
            row.Children.Add(showBtn);
            ContentPanel.Children.Add(row);

            ContentPanel.Children.Add(MakeDivider(new Thickness(0, 20, 0, 0)));
            ContentPanel.Children.Add(MakeSectionLabel("NOTE", new Thickness(0, 4, 0, 0)));
            ContentPanel.Children.Add(new TextBlock
            {
                Text = "Use !startmenu in terminal to enable ZeroLaunchpad (macOS-style app launcher).",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_MUTED)),
                FontSize = 11, TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });

            CategoryContent.Content = ContentPanel;
        }

        private void BuildLayoutsPanel()
        {
            var ContentPanel = new StackPanel();
            ContentPanel.Children.Add(MakeHeader("Layouts", "One-click style presets for your entire desktop"));
            ContentPanel.Children.Add(MakeDivider());

            // macOS Dock — blue/white identity
            AddPresetCard("🍎", "macOS Dock",
                "Transparent taskbar · Acrylic explorer · Glass notification · Desktop widget · ZeroLaunchpad",
                "#1A1E3A5C", "#3A2D5A8A", "#8BE9FD",
                WdmPresets.MacOSDock, launchWidget: true, enableLaunchpad: true);

            // Minimal Dark — pure black identity
            AddPresetCard("🌑", "Minimal Dark",
                "Fully transparent taskbar · Dark acrylic explorer · No distractions",
                "#1A1A1A1A", "#3A333337", "#AAAAAA",
                WdmPresets.MinimalDark, launchWidget: false, enableLaunchpad: false);

            // Cyberpunk — purple identity
            AddPresetCard("⚡", "Cyberpunk",
                "Purple acrylic taskbar · Purple explorer · Neon notification panel",
                "#1A2D0A3A", "#3A4A1A5A", "#FF79C6",
                WdmPresets.Cyberpunk, launchWidget: false, enableLaunchpad: false);

            // Classic Windows — dark blue identity
            AddPresetCard("🪟", "Classic Windows",
                "Dark acrylic taskbar · Light acrylic explorer · Standard look",
                "#1A0A1A2E", "#3A1A2A4E", "#8BE9FD",
                WdmPresets.ClassicWindows, launchWidget: false, enableLaunchpad: false);

            CategoryContent.Content = ContentPanel;
        }

        private Border AddPresetCard(string icon, string title, string desc,
            string bgHex, string borderHex, string accentHex,
            WdmState preset, bool launchWidget, bool enableLaunchpad)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderHex)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(16, 14, 16, 14),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Icon circle with accent color
            var iconBorder = new Border
            {
                Width = 36, Height = 36, CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    accentHex + "33")), // 20% opacity
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = new TextBlock
            {
                Text = icon, FontSize = 16,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // Text
            var textSp = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            textSp.Children.Add(new TextBlock
            {
                Text = title, FontSize = 14, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_PRI))
            });
            textSp.Children.Add(new TextBlock
            {
                Text = desc, FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_SEC)),
                Margin = new Thickness(0, 3, 0, 0),
                TextWrapping = System.Windows.TextWrapping.Wrap
            });
            Grid.SetColumn(textSp, 1);
            grid.Children.Add(textSp);

            // Arrow
            var arrow = new TextBlock
            {
                Text = "›", FontSize = 20,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentHex)),
                VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7
            };
            Grid.SetColumn(arrow, 2);
            grid.Children.Add(arrow);

            card.Child = grid;
            card.MouseLeftButtonDown += (s, e) => ApplyPreset(preset, launchWidget, enableLaunchpad);
            return card;
        }

        // MVVM-backed builders used by the DataTemplate/ContentControl approach
        private void BuildStylePanelMVVM(string cat)
        {
            WdmEntry? existing = null;
            if (cat == "Notification") _state.Entries.TryGetValue("__Notification__", out existing);
            else
            {
                var classes = WdmCategories.ClassMap.TryGetValue(cat, out var cls) ? cls : Array.Empty<string>();
                existing = classes.Select(c => _state.Entries.TryGetValue(c, out var e) ? e : null)
                                  .FirstOrDefault(e => e != null);
            }
            existing ??= new WdmEntry();

            string displayName = WdmCategories.DisplayNames.TryGetValue(cat, out var dn) ? dn : cat;
            var vm = new StylePanelViewModel
            {
                Title = displayName,
                Subtitle = GetCategoryDesc(cat),
                Styles = new ObservableCollection<string> { "Acrylic Dark", "Acrylic Light", "Blur Only", "Glass Clear", "Full Transparent", "Floating macOS" },
                SelectedStyleIndex = Math.Max(0, (int)existing.Style - 1),
                Opacity = existing.Alpha,
                ColorHex = existing.ColorHex ?? "#000000",
                HasActive = IsCategoryActive(cat),
                ActiveText = $"● Active — {existing.Style}  |  Opacity {(int)(existing.Alpha / 2.55)}%"
            };
            CategoryContent.Content = vm;
        }

        private void BuildLayoutsPanelMVVM()
        {
            var panel = new StackPanel();
            panel.Children.Add(MakeHeader("Layouts", "One-click style presets for your entire desktop"));
            panel.Children.Add(MakeDivider());

            panel.Children.Add(AddPresetCard("🍎", "macOS Dock",
                "Transparent taskbar · Acrylic explorer · Glass notification · Desktop widget · ZeroLaunchpad",
                "#1A1E3A5C", "#3A2D5A8A", "#8BE9FD",
                WdmPresets.MacOSDock, launchWidget: true, enableLaunchpad: true));

            panel.Children.Add(AddPresetCard("🌑", "Minimal Dark",
                "Fully transparent taskbar · Dark acrylic explorer · No distractions",
                "#1A1A1A1A", "#3A333337", "#AAAAAA",
                WdmPresets.MinimalDark, launchWidget: false, enableLaunchpad: false));

            panel.Children.Add(AddPresetCard("⚡", "Cyberpunk",
                "Purple acrylic taskbar · Purple explorer · Neon notification panel",
                "#1A2D0A3A", "#3A4A1A5A", "#FF79C6",
                WdmPresets.Cyberpunk, launchWidget: false, enableLaunchpad: false));

            panel.Children.Add(AddPresetCard("🪟", "Classic Windows",
                "Dark acrylic taskbar · Light acrylic explorer · Standard look",
                "#1A0A1A2E", "#3A1A2A4E", "#8BE9FD",
                WdmPresets.ClassicWindows, launchWidget: false, enableLaunchpad: false));

            CategoryContent.Content = panel;
        }

        private void BuildDesktopPanelMVVM()
        {
            var panel = new StackPanel();
            panel.Children.Add(MakeHeader("Desktop", "Manage desktop icons"));
            panel.Children.Add(MakeDivider());
            panel.Children.Add(MakeSectionLabel("DESKTOP ICONS"));

           var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var hideBtn = MakeCardButton("Hide Icons", "🙈");
            hideBtn.MouseLeftButtonDown += (s, e) => ShellHelper.HideDesktopIcons();
            row.Children.Add(hideBtn);
            var showBtn = MakeCardButton("Show Icons", "👁");
            showBtn.MouseLeftButtonDown += (s, e) => ShellHelper.ShowDesktopIcons();
            showBtn.Margin = new Thickness(10, 0, 0, 0);
            row.Children.Add(showBtn);
            panel.Children.Add(row);

            panel.Children.Add(MakeDivider(new Thickness(0, 20, 0, 0)));
            panel.Children.Add(MakeSectionLabel("NOTE", new Thickness(0, 4, 0, 0)));
            panel.Children.Add(new TextBlock
            {
                Text = "Use !startmenu in terminal to enable ZeroLaunchpad (macOS-style app launcher).",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_MUTED)),
                FontSize = 11, TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });

           CategoryContent.Content = panel;
        }

        private void ApplyPreset(WdmState preset, bool launchWidget, bool enableLaunchpad)
        {
            foreach (var kvp in preset.Entries)
            {
                _state.Entries[kvp.Key] = kvp.Value;
                if (kvp.Key == "__Notification__")
                    ShellHelper.ApplyNotificationStyle(kvp.Value);
                else
                    ShellHelper.EnumAllWindows((hwnd, cls) =>
                    {
                        if (cls == kvp.Key) ShellHelper.ApplyStyle(hwnd, kvp.Value);
                    });
            }

            // Explorer needs delayed apply
            if (preset.Entries.ContainsKey("CabinetWClass") || preset.Entries.ContainsKey("ExplorerWClass"))
            {
                var explorerEntry = preset.Entries.TryGetValue("CabinetWClass", out var ee) ? ee
                    : preset.Entries["ExplorerWClass"];
                _ = ShellHelper.ApplyExplorerStyleDelayed(explorerEntry);
            }

            // Launch desktop widget for macOS preset
            if (launchWidget)
            {
                if (_desktopWidgetInstance == null || !_desktopWidgetInstance.IsVisible)
                {
                    _desktopWidgetInstance = new ZeroMix.Widgets.DesktopWidget();
                    _desktopWidgetInstance.Show();
                }
            }

            // Enable ZeroLaunchpad (Start Menu interceptor) for macOS preset
            if (enableLaunchpad)
            {
                // Find the interceptor via the main ZeroShell window
                if (System.Windows.Application.Current?.MainWindow is ZeroShellWindow shell)
                    shell.EnableStartMenuInterceptor();
            }

            SaveState();
            BuildSidebar();
            UpdateStatusBar();
            EnsureWatcher();
        }
        #endregion

        #region Apply / Remove
        private async void ApplyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == "Layouts" || _selectedCategory == "Desktop") return;

            var entry = BuildEntryFromUI();
            if (entry == null) return;

            if (_selectedCategory == "Notification")
            {
                ShellHelper.ApplyNotificationStyle(entry);
                _state.Entries["__Notification__"] = entry;
            }
            else if (_selectedCategory == "Explorer")
            {
                // Apply immediately + delayed for newly opened windows
                var classes = WdmCategories.ClassMap["Explorer"];
                ShellHelper.EnumAllWindows((hwnd, cls) =>
                {
                    if (classes.Contains(cls))
                    {
                        ShellHelper.ApplyStyle(hwnd, entry);
                        ShellHelper.ApplyStyleToChildren(hwnd, entry);
                    }
                });
                await ShellHelper.ApplyExplorerStyleDelayed(entry);
                foreach (var c in classes) _state.Entries[c] = entry;
            }
            else
            {
                var classes = WdmCategories.ClassMap.TryGetValue(_selectedCategory, out var cls)
                    ? cls : Array.Empty<string>();
                ShellHelper.EnumAllWindows((hwnd, className) =>
                {
                    if (classes.Contains(className))
                    {
                        ShellHelper.ApplyStyle(hwnd, entry);
                        ShellHelper.ApplyStyleToChildren(hwnd, entry);
                    }
                });
                foreach (var c in classes) _state.Entries[c] = entry;
            }

            SaveState();
            BuildSidebar();
            SelectCategory(_selectedCategory);
            UpdateStatusBar();
            EnsureWatcher();
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCategory == "Layouts" || _selectedCategory == "Desktop") return;

            if (_selectedCategory == "Notification")
            {
                ShellHelper.ApplyNotificationStyle(new WdmEntry { Style = WdmStyle.None });
                _state.Entries.Remove("__Notification__");
            }
            else
            {
                var classes = WdmCategories.ClassMap.TryGetValue(_selectedCategory, out var cls)
                    ? cls : Array.Empty<string>();
                ShellHelper.EnumAllWindows((hwnd, className) =>
                {
                    if (classes.Contains(className)) ShellHelper.DisableAccent(hwnd);
                });
                foreach (var c in classes) _state.Entries.Remove(c);
            }

            SaveState();
            BuildSidebar();
            SelectCategory(_selectedCategory);
            UpdateStatusBar();
            EnsureWatcher();
        }

        private WdmEntry? BuildEntryFromUI()
        {
            // If MVVM style panel is active, read directly from ViewModel
            if (CategoryContent?.Content is StylePanelViewModel vm)
            {
                return vm.ToWdmEntry();
            }

            // Fallback: if the content is a panel built imperatively, inspect its children
            if (CategoryContent?.Content is System.Windows.Controls.Panel panel)
            {
                var styleCombo = panel.Children.OfType<ComboBox>().FirstOrDefault();
                var opSlider = panel.Children.OfType<Slider>().FirstOrDefault();
                var colorBox = panel.Children.OfType<TextBox>().FirstOrDefault();
                if (styleCombo == null) return null;
                return new WdmEntry
                {
                    Style = (WdmStyle)(styleCombo.SelectedIndex + 1),
                    Alpha = opSlider != null ? (int)opSlider.Value : 0xDD,
                    ColorHex = colorBox?.Text ?? "#000000",
                    AutoApply = true
                };
            }

            return null;
        }

        private void RestoreAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "This will restore all Windows styles to their default appearance.\n\nNo system files are modified. This is completely safe.",
                "Restore All to Default",
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Information);
            if (result != System.Windows.MessageBoxResult.OK) return;

            ShellHelper.EnumAllWindows((hwnd, cls) =>
            {
                switch (cls)
                {
                    case "Shell_TrayWnd": case "Shell_SecondaryTrayWnd":
                    case "CabinetWClass": case "ExplorerWClass":
                        ShellHelper.DisableAccent(hwnd); break;
                }
            });
            ShellHelper.ApplyNotificationStyle(new WdmEntry { Style = WdmStyle.None });
            ShellHelper.StopWatcher();
            CloseDesktopWidget();
            _state.Entries.Clear();
            SaveState();
            BuildSidebar();
            SelectCategory(_selectedCategory);
            UpdateStatusBar();

            System.Windows.MessageBox.Show(
                "✓ All styles restored to Windows default.",
                "Restore Complete",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.None);
        }
        #endregion

        #region Status Bar
        private void UpdateStatusBar()
        {
            int count = _state.Entries.Count;
            bool on = count > 0;
            if (ActiveSinceText != null)
                ActiveSinceText.Text = on ? $"● {count} active style{(count > 1 ? "s" : "")}" : "○ No active styles";
            if (WatcherStatusText != null)
                WatcherStatusText.Text = on ? "Watcher: ON" : "Watcher: OFF";
            if (StatusDot != null)
                StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(on ? C_GREEN : C_GREY_DOT));
            if (StatusText != null)
                StatusText.Text = on ? $"{count} active" : "Idle";
        }
        #endregion

        #region Watcher
        private void EnsureWatcher()
        {
            if (_state.Entries.Count == 0) { ShellHelper.StopWatcher(); return; }

            ShellHelper.StartWatcher((hwnd, cls) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_state.Entries.TryGetValue(cls, out var entry))
                    {
                        // Explorer: apply with delay
                        if (cls == "CabinetWClass" || cls == "ExplorerWClass")
                            _ = Task.Delay(300).ContinueWith(_ =>
                                Dispatcher.Invoke(() => ShellHelper.ApplyStyle(hwnd, entry)));
                        else
                            ShellHelper.ApplyStyle(hwnd, entry);
                        return;
                    }

                    // Notification
                    if (cls == "Windows.UI.Core.CoreWindow" &&
                        _state.Entries.TryGetValue("__Notification__", out var notifEntry))
                    {
                        ShellHelper.GetWindowThreadProcessId(hwnd, out uint pid);
                        foreach (var p in System.Diagnostics.Process.GetProcessesByName("ShellExperienceHost"))
                        {
                            if ((uint)p.Id == pid)
                            {
                                ShellHelper.ApplyStyle(hwnd, notifEntry);
                                ShellHelper.ApplyStyleToChildren(hwnd, notifEntry);
                                break;
                            }
                        }
                    }
                });
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            ShellHelper.StopWatcher();
            base.OnClosed(e);
        }
        #endregion

        #region Persistence
        private void LoadState()
        {
            try
            {
                if (File.Exists(_statePath))
                    _state = JsonSerializer.Deserialize<WdmState>(File.ReadAllText(_statePath)) ?? new WdmState();
            }
            catch { _state = new WdmState(); }
        }

        private void SaveState()
        {
            try
            {
                var dir = IOPath.GetDirectoryName(_statePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_statePath, JsonSerializer.Serialize(_state,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
        #endregion

        #region Desktop Widget
        private static ZeroMix.Widgets.DesktopWidget? _desktopWidgetInstance;

        private void CloseDesktopWidget()
        {
            _desktopWidgetInstance?.Shutdown();
            _desktopWidgetInstance = null;
        }
        #endregion

        #region UI Helpers
        private StackPanel MakeHeader(string title, string subtitle)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
            sp.Children.Add(new TextBlock
            {
                Text = title, FontSize = 20, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_PRI))
            });
            if (!string.IsNullOrEmpty(subtitle))
                sp.Children.Add(new TextBlock
                {
                    Text = subtitle, FontSize = 11,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_SEC)),
                    Margin = new Thickness(0, 3, 0, 0)
                });
            return sp;
        }

        private TextBlock MakeSectionLabel(string text, Thickness? margin = null) => new TextBlock
        {
            Text = text, FontSize = 10, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_MUTED)),
            Margin = margin ?? new Thickness(0)
        };

        private Border MakeDivider(Thickness? margin = null) => new Border
        {
            Height = 1,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_BORDER)),
            Margin = margin ?? new Thickness(0, 16, 0, 0)
        };

        private Border MakeCardButton(string label, string icon)
        {
            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333337")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_BORDER)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10), Cursor = Cursors.Hand
            };
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new TextBlock { Text = icon, FontSize = 14, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
            sp.Children.Add(new TextBlock
            {
                Text = label, FontSize = 12,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(C_TEXT_PRI)),
                VerticalAlignment = VerticalAlignment.Center
            });
            card.Child = sp;
            return card;
        }
        #endregion

    }

    // ViewModel for the style panel (bound via DataTemplate in XAML)
    public class StylePanelViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public ObservableCollection<string> Styles { get; set; } = new ObservableCollection<string>();

        private int _selectedStyleIndex = 0;
        public int SelectedStyleIndex { get => _selectedStyleIndex; set { _selectedStyleIndex = value; OnPropertyChanged(nameof(SelectedStyleIndex)); } }

        private int _opacity = 220;
        public int Opacity { get => _opacity; set { _opacity = value; OnPropertyChanged(nameof(Opacity)); OnPropertyChanged(nameof(OpacityPercent)); } }

        public int OpacityPercent => (int)(Opacity / 2.55);

        private string _colorHex = "#000000";
        public string ColorHex { get => _colorHex; set { _colorHex = value; OnPropertyChanged(nameof(ColorHex)); } }

        public bool HasActive { get; set; } = false;
        public string ActiveText { get; set; } = "";

        public WdmEntry ToWdmEntry()
        {
            return new WdmEntry { Style = (WdmStyle)(SelectedStyleIndex + 1), Alpha = Opacity, ColorHex = ColorHex, AutoApply = true };
        }
    }
}
