using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
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

                // Hapus tulisan asli (Mundur perlahan, tidak lag)
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
                // Jalur Utama (GTX)
                string urlGTX = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={Uri.EscapeDataString(input)}";
                
                var resp = await _httpClient.GetAsync(urlGTX);
                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    if (!json.Contains("<html")) // Anti Captcha HTML Block
                    {
                        var matches = Regex.Matches(json, "\"(.*?)\"");
                        if (matches.Count > 0) return matches[0].Groups[1].Value;
                    }
                }

                // API Cadangan Darurat (MyMemory Bebas Captcha & Bebas Blokir Limit)
                string urlMM = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(input)}&langpair={from}|{to}";
                var respMM = await _httpClient.GetAsync(urlMM);
                if (respMM.IsSuccessStatusCode)
                {
                    string json = await respMM.Content.ReadAsStringAsync();
                    var match = Regex.Match(json, "\"translatedText\":\"(.*?)\"");
                    if (match.Success) return Regex.Unescape(match.Groups[1].Value);
                }

                return "[ERROR] Akses Publik Google Sedang Dibatasi IP-nya.";
            }
            catch (Exception ex)
            {
                return $"[ERROR] C-Engine: {ex.Message}";
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
