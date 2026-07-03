using System;
using System.Collections.Generic;
using System.Windows;

namespace zeromix.SnapLayout.Models
{
    /// <summary>
    /// Layout preset identifiers for quick selection and persistence.
    /// </summary>
    public enum SnapLayoutPreset
    {
        TwoColumns,   // Kiri 50%, Kanan 50%
        ThreeColumns, // 33%, 33%, 33%
        TwoPlusOne,   // Kiri 67%, Kanan 33%
        OnePlusTwo,   // Kiri 33%, Kanan 67%
        TwoByTwo,     // 4 kuadran
        TopBottom     // Atas 50%, Bawah 50%
    }

    /// <summary>
    /// Helper statis untuk menghasilkan daftar SnapZone dari suatu preset.
    /// Semua kalkulasi berdasarkan SystemParameters.WorkArea secara default,
    /// dengan overload yang menerima Rect untuk support multi-monitor.
    /// </summary>
    public static class SnapLayoutHelper
    {
        /// <summary>
        /// Generate zones untuk preset tertentu dalam bounds yang diberikan.
        /// </summary>
        public static List<SnapZone> GetZones(SnapLayoutPreset preset, Rect bounds)
        {
            double w = bounds.Width;
            double h = bounds.Height;
            double x = bounds.X;
            double y = bounds.Y;

            return preset switch
            {
                SnapLayoutPreset.TwoColumns => new List<SnapZone>
                {
                    new(0, "Kiri 50%",  new Rect(x,       y, w * 0.5, h)),
                    new(1, "Kanan 50%", new Rect(x+w*0.5, y, w * 0.5, h)),
                },

                SnapLayoutPreset.ThreeColumns => new List<SnapZone>
                {
                    new(0, "Kiri 33%",  new Rect(x,          y, w / 3, h)),
                    new(1, "Tengah 33%",new Rect(x + w/3,    y, w / 3, h)),
                    new(2, "Kanan 33%", new Rect(x + w*2/3,  y, w / 3, h)),
                },

                SnapLayoutPreset.TwoPlusOne => new List<SnapZone>
                {
                    new(0, "Kiri 67%",  new Rect(x,          y, w * 2 / 3, h)),
                    new(1, "Kanan 33%", new Rect(x+w*2/3,    y, w / 3,     h)),
                },

                SnapLayoutPreset.OnePlusTwo => new List<SnapZone>
                {
                    new(0, "Kiri 33%",  new Rect(x,          y, w / 3,     h)),
                    new(1, "Kanan 67%", new Rect(x + w/3,    y, w * 2 / 3, h)),
                },

                SnapLayoutPreset.TwoByTwo => new List<SnapZone>
                {
                    new(0, "TL 50x50", new Rect(x,       y,       w * 0.5, h * 0.5)),
                    new(1, "TR 50x50", new Rect(x+w*0.5, y,       w * 0.5, h * 0.5)),
                    new(2, "BL 50x50", new Rect(x,       y+h*0.5, w * 0.5, h * 0.5)),
                    new(3, "BR 50x50", new Rect(x+w*0.5, y+h*0.5, w * 0.5, h * 0.5)),
                },

                SnapLayoutPreset.TopBottom => new List<SnapZone>
                {
                    new(0, "Atas 50%", new Rect(x, y,       w, h * 0.5)),
                    new(1, "Bawah 50%",new Rect(x, y+h*0.5, w, h * 0.5)),
                },

                _ => GetZones(SnapLayoutPreset.TwoColumns, bounds),
            };
        }

        /// <summary>
        /// Generate zones untuk primary work area (SystemParameters.WorkArea).
        /// </summary>
        public static List<SnapZone> GetZones(SnapLayoutPreset preset)
        {
            var workArea = SystemParameters.WorkArea;
            return GetZones(preset, new Rect(workArea.Left, workArea.Top, workArea.Width, workArea.Height));
        }

        /// <summary>
        /// Dapatkan preset dari string nama.
        /// </summary>
        public static SnapLayoutPreset ParsePreset(string name)
        {
            var lower = name?.ToLower().Trim() ?? "";
            return lower switch
            {
                "2col" or "twocolumns" => SnapLayoutPreset.TwoColumns,
                "3col" or "threecolumns" => SnapLayoutPreset.ThreeColumns,
                "2+1" or "twoplusone" => SnapLayoutPreset.TwoPlusOne,
                "1+2" or "oneplustwo" => SnapLayoutPreset.OnePlusTwo,
                "2x2" or "twobytwo" => SnapLayoutPreset.TwoByTwo,
                "topbottom" or "tb" => SnapLayoutPreset.TopBottom,
                _ => Enum.TryParse(name, true, out SnapLayoutPreset parsed) ? parsed : SnapLayoutPreset.TwoColumns,
            };
        }
    }
}
