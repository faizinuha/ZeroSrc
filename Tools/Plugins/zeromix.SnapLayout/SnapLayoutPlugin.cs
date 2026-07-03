namespace zeromix.SnapLayout
{
    /// <summary>
    /// Entry point plugin Snap Layout — mengikuti pola CatGatekeeperPlugin.
    /// Mengelola lifecycle SnapLayoutService dan UI.
    /// </summary>
    public class SnapLayoutPlugin
    {
        public string Name => "Snap Layout";
        public string Version => "1.0.0";
        public string Author => "ZeroMix";
        public string Description => "FancyZones-style window snapping — drag-to-zone and keybind overlay.";

        private SnapLayoutService? _service;
        private SnapLayoutUI? _ui;

        public SnapLayoutService? Service => _service;

        public SnapLayoutUI GetUI()
        {
            if (_service == null)
            {
                _service = new SnapLayoutService();
                _ui = new SnapLayoutUI(_service);
            }
            return _ui!;
        }

        public void Dispose()
        {
            _service?.Dispose();
            _service = null;
            _ui = null;
        }
    }
}
