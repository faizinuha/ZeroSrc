using System.Windows;

namespace ZeroMix.Recorder
{
    public partial class RecordingBorderWindow : Window
    {
        public RecordingBorderWindow(Rect area)
        {
            InitializeComponent();
            
            // Set position and size based on the recording area
            this.Left = area.Left - 2;
            this.Top = area.Top - 2;
            this.Width = area.Width + 4;
            this.Height = area.Height + 4;
            
            // Subtle glow animation could be added here
        }
    }
}
