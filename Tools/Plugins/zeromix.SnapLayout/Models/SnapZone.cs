using System.Windows;

namespace zeromix.SnapLayout.Models
{
    /// <summary>
    /// Satu zona snap — persegi panjang target tempat window akan di-snap.
    /// Koordinat dalam pixel (screen-space), sudah termasuk DPI scaling.
    /// </summary>
    public class SnapZone
    {
        public int Index { get; set; }
        public string Label { get; set; } = "";
        public Rect ScreenRect { get; set; }

        public SnapZone() { }

        public SnapZone(int index, string label, Rect screenRect)
        {
            Index = index;
            Label = label;
            ScreenRect = screenRect;
        }

        public override string ToString() => $"#{Index} {Label} ({ScreenRect.Width:F0}x{ScreenRect.Height:F0})";
    }
}
