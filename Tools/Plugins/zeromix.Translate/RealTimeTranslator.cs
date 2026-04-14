using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms; // Butuh reference ke System.Windows.Forms di wpf untuk Keys enum.

namespace ZeroMix.Plugins.Translate
{
    public class RealTimeTranslator : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private StringBuilder _buffer = new StringBuilder();
        private System.Timers.Timer _debounceTimer;
        private string _currentSyncToken = "";
        private bool _isSystemInjecting = false;

        public string SourceLang { get; set; } = "id";
        public string TargetLang { get; set; } = "en";
        public int DebounceMs { get; set; } = 800; // Customizable delay

        // Mode game: pakai clipboard paste alih-alih inject Unicode langsung
        // Aktifkan ini jika target adalah game (DirectInput/RawInput)
        public bool GameMode { get; set; } = false;

        private static readonly HttpClient _httpClient = new HttpClient();

        public event Action<string> OnError;
        public event Action<string, string> OnTranslated;

        static RealTimeTranslator()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public RealTimeTranslator()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);

            _debounceTimer = new System.Timers.Timer(DebounceMs);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += async (s, e) => await TriggerTranslationAsync();
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                int flags = Marshal.ReadInt32(lParam, 8); // Offset 8 adalah flags

                // CEK: Apakah ini tombol hasil simulasi kita sendiri (Injected)?
                // Kita HARUS membiarkan tombol ini lewat agar sampai ke aplikasi target.
                bool isInjected = (flags & 0x10) != 0;
                if (isInjected) return CallNextHookEx(_hookID, nCode, wParam, lParam);

                // Jika sedang menyuntik teks terjemahan, BLOKIR input fisik user
                // agar tidak terjadi "tabrakan" karakter di layar.
                if (_isSystemInjecting) 
                {
                    return (IntPtr)1; 
                }

                Keys key = (Keys)vkCode;

                if (key == Keys.Back)
                {
                    if (_buffer.Length > 0)
                    {
                        _buffer.Length--;
                        ResetTimer();
                    }
                }
                else if (key == Keys.Escape || key == Keys.Enter || key == Keys.Tab)
                {
                     // Force clear
                     ClearBuffer();
                }
                else
                {
                    char c = ConvertToChar(vkCode);
                    if (c != '\0' && !char.IsControl(c))
                    {
                        _buffer.Append(c);
                        ResetTimer();
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void ResetTimer()
        {
            _debounceTimer.Stop();
            // Bikin token baru setiap kali ada huruf baru yang diketik.
            // Ini jantung dari Anti-Tabrakan sistem.
            _currentSyncToken = Guid.NewGuid().ToString(); 
            _debounceTimer.Interval = DebounceMs;
            _debounceTimer.Start();
        }

        private void ClearBuffer()
        {
            _buffer.Clear();
            _debounceTimer.Stop();
            _currentSyncToken = "";
        }

        private async Task TriggerTranslationAsync()
        {
            string textToTranslate = _buffer.ToString().Trim();
            if (string.IsNullOrEmpty(textToTranslate)) return;

            // Simpan token saat API mulai dipanggil
            string tokenAtStart = _currentSyncToken;

            string translated = await TranslateApiAsync(textToTranslate, SourceLang, TargetLang);

            // KRUSIAL: Jika saat API loading, user ngetik tombol baru (Token berubah),
            // maka kita ABORT proses tulisan ini supaya tidak hancur lebur / ngaco di layar!
            if (_currentSyncToken != tokenAtStart) return;
            
            if (translated == textToTranslate || string.IsNullOrEmpty(translated) || translated.StartsWith("[ERROR]"))
            {
                if (translated.StartsWith("[ERROR]")) OnError?.Invoke(translated);
                return;
            }

            // Memasuki Fase Injeksi: Keylogger mengambil alih akses Windows
            _isSystemInjecting = true;
            try
            {
                // Beri tahu UI buat Live Monitoring
                OnTranslated?.Invoke(textToTranslate, translated);

                if (GameMode)
                {
                    // === MODE GAME: Clipboard Paste ===
                    // Hapus tulisan asli dulu pakai backspace
                    for (int i = 0; i < textToTranslate.Length; i++)
                    {
                        SendKey(0x08); // Backspace
                        await Task.Delay(15);
                    }

                    // Taruh hasil terjemahan ke clipboard lalu paste via Ctrl+V
                    // Ini bekerja di hampir semua game chat karena Ctrl+V adalah
                    // shortcut OS-level yang diproses sebelum game engine
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        System.Windows.Clipboard.SetText(translated);
                    });

                    await Task.Delay(50); // Beri waktu clipboard settle

                    // Kirim Ctrl+V via scan code (lebih diterima DirectInput)
                    SendCtrlV();
                }
                else
                {
                    // === MODE NORMAL: Unicode Inject (browser, notepad, dll) ===
                    // Hapus tulisan asli
                    for (int i = 0; i < textToTranslate.Length; i++)
                    {
                        SendKey(0x08); // Backspace
                        await Task.Delay(10);
                    }

                    // Tulis hasil Translate (Pakai Unicode Bypass Native)
                    foreach (char c in translated)
                    {
                        SendUnicodeChar(c);
                        await Task.Delay(10);
                    }
                }
            }
            finally
            {
                // Injeksi selesai, kembalikan kontrol keyboard ke User System
                _buffer.Clear();
                _isSystemInjecting = false;
            }
        }

        public async Task<string> TranslateApiAsync(string input, string from, string to)
        {
            try
            {
                // ── Jalur 1: Google Translate (GTX) dengan parsing JSON yang benar ──
                string urlGTX = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={Uri.EscapeDataString(input)}";
                var resp = await _httpClient.GetAsync(urlGTX);
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    if (!json.Contains("<html"))
                    {
                        // Format: [[[\"translated\",\"original\",...],...],...] 
                        // Ambil semua segmen terjemahan dari array pertama
                        var result = ParseGoogleTranslateJson(json);
                        if (!string.IsNullOrWhiteSpace(result) && result != input)
                            return result;
                    }
                }

                // ── Jalur 2: Google Translate Web API (client=dict-chrome-ex) ──
                string urlGT2 = $"https://translate.googleapis.com/translate_a/single?client=dict-chrome-ex&sl={from}&tl={to}&dt=t&q={Uri.EscapeDataString(input)}";
                var resp2 = await _httpClient.GetAsync(urlGT2);
                if (resp2.IsSuccessStatusCode)
                {
                    string json2 = await resp2.Content.ReadAsStringAsync();
                    if (!json2.Contains("<html"))
                    {
                        var result2 = ParseGoogleTranslateJson(json2);
                        if (!string.IsNullOrWhiteSpace(result2) && result2 != input)
                            return result2;
                    }
                }

                // ── Jalur 3: MyMemory (bebas captcha, 5000 kata/hari gratis) ──
                // MyMemory tidak support "auto" — skip jika source lang auto
                if (from != "auto")
                {
                    string urlMM = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(input)}&langpair={from}|{to}&de=zeromix@translate.app";
                    var respMM = await _httpClient.GetAsync(urlMM);
                    if (respMM.IsSuccessStatusCode)
                    {
                        string jsonMM = await respMM.Content.ReadAsStringAsync();
                        var matchMM = Regex.Match(jsonMM, "\"translatedText\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                        if (matchMM.Success)
                        {
                            string translated = Regex.Unescape(matchMM.Groups[1].Value);
                            if (!translated.StartsWith("QUERY") && translated != input)
                                return translated;
                        }
                    }
                }

                return "[ERROR] Semua API tidak dapat dijangkau. Cek koneksi internet.";
            }
            catch (TaskCanceledException)
            {
                return "[ERROR] Timeout — koneksi terlalu lambat.";
            }
            catch (Exception ex)
            {
                return $"[ERROR] {ex.Message}";
            }
        }

        /// <summary>
        /// Parse format JSON Google Translate:
        /// [[[seg1_translated, seg1_original], [seg2_translated, seg2_original], ...], ...]
        /// Contoh: [[["Good morning","selamat pagi",null,null,10]],null,"id",...]
        /// Ambil HANYA dari array pertama (index 0), abaikan metadata di belakang.
        /// </summary>
        private static string ParseGoogleTranslateJson(string json)
        {
            try
            {
                // Cari array pertama: [[[...]]]
                // Ambil konten antara [[[ dan ]]]
                int start = json.IndexOf("[[[");
                if (start == -1) return string.Empty;
                
                int end = json.IndexOf("]]]", start);
                if (end == -1) return string.Empty;

                string firstArray = json.Substring(start + 3, end - start - 3);
                
                var sb = new StringBuilder();
                // Sekarang parse segmen: ["translated","original",...]
                // Ambil string pertama dari setiap sub-array
                var segments = Regex.Matches(firstArray, @"\[""((?:[^""\\]|\\.)*?)""");
                foreach (Match m in segments)
                {
                    string seg = m.Groups[1].Value
                        .Replace("\\n", "\n")
                        .Replace("\\t", "\t")
                        .Replace("\\\"", "\"")
                        .Replace("\\\\", "\\");
                    if (!string.IsNullOrWhiteSpace(seg))
                        sb.Append(seg);
                }
                return sb.ToString().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private char ConvertToChar(int vkCode)
        {
            byte[] keyState = new byte[256];
            if (!GetKeyboardState(keyState)) return '\0';

            StringBuilder sb = new StringBuilder(2);
            uint scanCode = MapVirtualKey((uint)vkCode, 0);
            int result = ToUnicode((uint)vkCode, scanCode, keyState, sb, sb.Capacity, 0);

            if (result > 0) return sb[0];
            return '\0';
        }

        private void SendKey(short vk)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = 1; inputs[0].u.ki.wVk = vk;
            inputs[1].type = 1; inputs[1].u.ki.wVk = vk; inputs[1].u.ki.dwFlags = 2; // KEYUP
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // Kirim Ctrl+V via virtual key (paste clipboard)
        // Bekerja di game chat karena diproses di level OS/window message
        private void SendCtrlV()
        {
            INPUT[] inputs = new INPUT[4];
            // Ctrl DOWN
            inputs[0].type = 1; inputs[0].u.ki.wVk = 0x11; // VK_CONTROL
            // V DOWN
            inputs[1].type = 1; inputs[1].u.ki.wVk = 0x56; // VK_V
            // V UP
            inputs[2].type = 1; inputs[2].u.ki.wVk = 0x56; inputs[2].u.ki.dwFlags = 2;
            // Ctrl UP
            inputs[3].type = 1; inputs[3].u.ki.wVk = 0x11; inputs[3].u.ki.dwFlags = 2;
            SendInput(4, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SendUnicodeChar(char c)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = 1; inputs[0].u.ki.wScan = (short)c; inputs[0].u.ki.dwFlags = 4; // UNICODE
            inputs[1].type = 1; inputs[1].u.ki.wScan = (short)c; inputs[1].u.ki.dwFlags = 4 | 2; // UNICODE | KEYUP
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        public void ForceClear() => ClearBuffer();

        public void Dispose()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            _debounceTimer?.Dispose();
        }

        // P/Invoke WinAPI
        [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] private static extern bool GetKeyboardState(byte[] lpKeyState);
        [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [DllImport("user32.dll", ExactSpelling = true)] private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
        [DllImport("user32.dll")] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)] struct INPUT { public int type; public InputUnion u; }
        [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; [FieldOffset(0)] public HARDWAREINPUT hi; }
        [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public short wVk; public short wScan; public uint dwFlags; public int time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx; public int dy; public int mouseData; public int dwFlags; public int time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)] struct HARDWAREINPUT { public int uMsg; public short wParamL; public short wParamH; }
    }
}
