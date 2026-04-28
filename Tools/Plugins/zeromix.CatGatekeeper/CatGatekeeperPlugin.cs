/*
 * Cat Gatekeeper Plugin for ZeroMix
 *
 * Aset Asli oleh: @konekone2026 (ZOKUZOKU) – Cat Gatekeeper
 * Ekstraksi & Optimasi: Zaki
 * Sumber: https://x.com/konekone2026
 */

namespace zeromix.CatGatekeeper
{
    public class CatGatekeeperPlugin
    {
        public string Name        => "Cat Gatekeeper";
        public string Version     => "1.0.0";
        public string Author      => "Zaki (Aset: @konekone2026)";
        public string Description => "Kucing lucu yang memaksa kamu istirahat setelah terlalu lama di depan layar.";

        private CatGatekeeperUI?      _ui;
        private CatGatekeeperService? _service;

        public CatGatekeeperUI GetUI()
        {
            if (_service == null)
            {
                _service = new CatGatekeeperService();
                _ui      = new CatGatekeeperUI(_service);
                _service.Start();
            }
            return _ui!;
        }

        public void Dispose()
        {
            _service?.Dispose();
            _service = null;
            _ui      = null;
        }
    }
}
