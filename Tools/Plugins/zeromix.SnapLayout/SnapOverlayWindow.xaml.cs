using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using zeromix.SnapLayout.Models;

using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using Point = System.Windows.Point;

namespace zeromix.SnapLayout
{
    /// <summary>
    /// Fullscreen overlay window muncul saat keybind Ctrl+Win+Z.
    /// Fake transparency via screenshot background, render zona grid yang bisa diklik.
    /// </summary>
    public partial class SnapOverlayWindow : Window
    {
        private SnapLayoutPreset _selectedPreset;
        private readonly Rect _screenBounds;
        private readonly Action<SnapZone> _onZoneSelected;
        private bool _isClosed;

        // Layout preset buttons
        private readonly Dictionary<Border, SnapLayoutPreset> _layoutButtons = new();
        private readonly List<Border> _buttonOrder = new();
        private int _focusedIndex;

        // Current zones for hit-testing on canvas click
        private List<SnapZone> _currentZones = new();

        // Zone preview rectangles on canvas — mapped to SnapZone
        private readonly Dictionary<Border, SnapZone> _previewRects = new();

        public SnapOverlayWindow(SnapLayoutPreset initialPreset, Rect screenBounds, Action<SnapZone> onZoneSelected)
        {
            InitializeComponent();
            _selectedPreset = initialPreset;
            _screenBounds = screenBounds;
            _onZoneSelected = onZoneSelected;

            this.Left = screenBounds.X;
            this.Top = screenBounds.Y;
            this.Width = screenBounds.Width;
            this.Height = screenBounds.Height;

            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CaptureBackground();
            BuildLayoutButtons();
            HighlightPreset(_selectedPreset);
            ShowZonePreview(_selectedPreset);

            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120));
            this.BeginAnimation(OpacityProperty, fadeIn);

            var scaleIn = new DoubleAnimation(0.9, 1.0, TimeSpan.FromMilliseconds(150));
            scaleIn.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
            CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
        }

        private void CaptureBackground()
        {
            try
            {
                double left = SystemParameters.VirtualScreenLeft;
                double top = SystemParameters.VirtualScreenTop;
                double width = SystemParameters.VirtualScreenWidth;
                double height = SystemParameters.VirtualScreenHeight;

                using (var bmp = new System.Drawing.Bitmap((int)width, (int)height))
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen((int)left, (int)top, 0, 0,
                        new System.Drawing.Size((int)width, (int)height));

                    using (var ms = new System.IO.MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        ms.Seek(0, System.IO.SeekOrigin.Begin);

                        var decoder = new PngBitmapDecoder(ms,
                            BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                        BgImage.Source = decoder.Frames[0];
                    }
                }
            }
            catch
            {
                BgImage.Source = null;
            }
        }

        #region Layout Buttons

        private void BuildLayoutButtons()
        {
            var row1 = new[] { (SnapLayoutPreset.TwoColumns, "50/50"), (SnapLayoutPreset.ThreeColumns, "33/33/33"), (SnapLayoutPreset.TwoPlusOne, "67/33"), (SnapLayoutPreset.OnePlusTwo, "33/67") };
            foreach (var (p, l) in row1) LayoutRow1.Children.Add(MakeBtn(p, l));

            var row2 = new[] { (SnapLayoutPreset.TwoByTwo, "2×2"), (SnapLayoutPreset.TopBottom, "50/50") };
            foreach (var (p, l) in row2) LayoutRow2.Children.Add(MakeBtn(p, l));

            for (int i = 0; i < 2; i++) LayoutRow2.Children.Add(MakeEmptySlot());
        }

        private Border MakeBtn(SnapLayoutPreset preset, string label)
        {
            var b = new Border
            {
                Width = 80, Height = 60, Margin = new Thickness(4),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(0x15, 0x00, 0xD4, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x30, 0x1E, 0x26, 0x33)),
                BorderThickness = new Thickness(1), Cursor = Cursors.Hand, Tag = preset
            };

            var cv = new Canvas { Width = 60, Height = 32, Margin = new Thickness(0, 2, 0, 0) };
            DrawMiniZones(cv, preset);
            b.Child = new StackPanel { Children = { cv, new TextBlock { Text = label, FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(0x7A, 0x84, 0x94)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) } } };

            b.MouseEnter += (_, _) => { if (preset != _selectedPreset) { b.Background = new SolidColorBrush(Color.FromArgb(0x30, 0x00, 0xD4, 0xFF)); b.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xD4, 0xFF)); } ShowZonePreview(preset); };
            b.MouseLeave += (_, _) => { if (preset != _selectedPreset) { b.Background = new SolidColorBrush(Color.FromArgb(0x15, 0x00, 0xD4, 0xFF)); b.BorderBrush = new SolidColorBrush(Color.FromArgb(0x30, 0x1E, 0x26, 0x33)); } };

            b.MouseLeftButtonDown += (_, e) =>
            {
                _selectedPreset = preset;
                HighlightPreset(preset);
                ShowZonePreview(preset);
                e.Handled = true;
            };

            _layoutButtons[b] = preset;
            _buttonOrder.Add(b);
            return b;
        }

        private static Border MakeEmptySlot() => new()
        {
            Width = 80, Height = 60, Margin = new Thickness(4), CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromArgb(0x08, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(0), Opacity = 0.3,
            Child = new TextBlock { Text = "—", Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x54, 0x68)), FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };

        private void DrawMiniZones(Canvas cv, SnapLayoutPreset preset)
        {
            var zones = SnapLayoutHelper.GetZones(preset, new Rect(0, 0, cv.Width, cv.Height));
            foreach (var z in zones)
            {
                var r = new System.Windows.Shapes.Rectangle { Width = z.ScreenRect.Width, Height = z.ScreenRect.Height, Fill = new SolidColorBrush(Color.FromArgb(0x60, 0x00, 0xD4, 0xFF)), RadiusX = 2, RadiusY = 2 };
                Canvas.SetLeft(r, z.ScreenRect.X); Canvas.SetTop(r, z.ScreenRect.Y);
                cv.Children.Add(r);
            }
        }

        private void HighlightPreset(SnapLayoutPreset preset)
        {
            foreach (var (btn, p) in _layoutButtons)
            {
                Color bg = Color.FromArgb(0x30, 0x00, 0xD4, 0xFF);
                Color brd = Color.FromRgb(0x00, 0xD4, 0xFF);
                if (p != preset)
                {
                    bg = Color.FromArgb(0x15, 0x00, 0xD4, 0xFF);
                    brd = Color.FromArgb(0x30, 0x1E, 0x26, 0x33);
                }
                btn.Background = new SolidColorBrush(bg);
                btn.BorderBrush = new SolidColorBrush(brd);
            }
        }

        #endregion

        #region Zone Preview on Canvas (Clickable)

        private void ShowZonePreview(SnapLayoutPreset preset)
        {
            foreach (var r in _previewRects.Keys)
                PreviewCanvas.Children.Remove(r);
            _previewRects.Clear();

            _currentZones = SnapLayoutHelper.GetZones(preset, _screenBounds);

            foreach (var zone in _currentZones)
            {
                var rect = new Border
                {
                    Width = zone.ScreenRect.Width,
                    Height = zone.ScreenRect.Height,
                    Background = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0xD4, 0xFF)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xD4, 0xFF)),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    Tag = zone.Index,
                    ToolTip = zone.Label
                };

                rect.MouseEnter += (_, _) => { rect.Background = new SolidColorBrush(Color.FromArgb(0x50, 0x00, 0xD4, 0xFF)); };
                rect.MouseLeave += (_, _) => { rect.Background = new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0xD4, 0xFF)); };
                rect.MouseLeftButtonDown += (_, _) => { SelectZoneAndClose(zone); };

                Canvas.SetLeft(rect, zone.ScreenRect.X - _screenBounds.X);
                Canvas.SetTop(rect, zone.ScreenRect.Y - _screenBounds.Y);
                PreviewCanvas.Children.Add(rect);
                _previewRects[rect] = zone;
            }
        }

        private void PreviewCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(PreviewCanvas);
            double screenX = pos.X + _screenBounds.X;
            double screenY = pos.Y + _screenBounds.Y;
            var pt = new Point(screenX, screenY);

            var hit = _currentZones.FirstOrDefault(z => z.ScreenRect.Contains(pt));
            if (hit != null)
            {
                SelectZoneAndClose(hit);
                e.Handled = true;
            }
        }

        #endregion

        #region Close / Snap

        private void SelectZoneAndClose(SnapZone zone)
        {
            if (_isClosed) return;
            _isClosed = true;

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (_, _) =>
            {
                try { Close(); } catch { }
                _onZoneSelected?.Invoke(zone);
            };
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void CloseWithoutSnap()
        {
            if (_isClosed) return;
            _isClosed = true;

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
            fadeOut.Completed += (_, _) => { try { Close(); } catch { } };
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        #endregion

        #region Input Handlers

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape) { CloseWithoutSnap(); e.Handled = true; return; }

            if (e.Key >= System.Windows.Input.Key.D1 && e.Key <= System.Windows.Input.Key.D6)
            {
                int idx = (int)(e.Key - System.Windows.Input.Key.D1);
                var presets = (SnapLayoutPreset[])Enum.GetValues(typeof(SnapLayoutPreset));
                if (idx < presets.Length)
                {
                    _selectedPreset = presets[idx];
                    HighlightPreset(_selectedPreset);
                    ShowZonePreview(_selectedPreset);
                }
                e.Handled = true;
                return;
            }

            if (e.Key == System.Windows.Input.Key.Right || e.Key == System.Windows.Input.Key.Down)
            {
                _focusedIndex = (_focusedIndex + 1) % _buttonOrder.Count;
                _buttonOrder[_focusedIndex].Focus();
                ShowZonePreview((SnapLayoutPreset)_buttonOrder[_focusedIndex].Tag);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Left || e.Key == System.Windows.Input.Key.Up)
            {
                _focusedIndex = (_focusedIndex - 1 + _buttonOrder.Count) % _buttonOrder.Count;
                _buttonOrder[_focusedIndex].Focus();
                ShowZonePreview((SnapLayoutPreset)_buttonOrder[_focusedIndex].Tag);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                var zones = SnapLayoutHelper.GetZones(_selectedPreset, _screenBounds);
                if (zones.Count > 0) SelectZoneAndClose(zones[0]);
                e.Handled = true;
            }
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(CardBorder);
            if (pos.X < 0 || pos.Y < 0 || pos.X > CardBorder.ActualWidth || pos.Y > CardBorder.ActualHeight)
            {
                CloseWithoutSnap();
                e.Handled = true;
            }
        }

        #endregion
    }
}
