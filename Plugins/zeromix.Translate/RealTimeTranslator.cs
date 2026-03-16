using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ZeroMix.Plugins.Translate
{
    public class RealTimeTranslator : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int VK_SPACE = 0x20;
        private const int VK_RETURN = 0x0D;
        private const int VK_BACK = 0x08;
        private const int VK_ESCAPE = 0x1B;

        private System.Timers.Timer _autoTranslateTimer;
        private bool _isProcessing = false;
        private bool _isInternalSend = false;

        public RealTimeTranslator()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            
            // Timer untuk deteksi jeda ketik (Pure Magic Mode)
            _autoTranslateTimer = new System.Timers.Timer(700); // Jeda 700ms
            _autoTranslateTimer.AutoReset = false;
            _autoTranslateTimer.Elapsed += (s, e) => {
                string text = _buffer.ToString().Trim();
                if (!string.IsNullOrEmpty(text) && !_isProcessing)
                {
                    _isProcessing = true;
                    _buffer.Clear();
                    Task.Run(() => ProcessTranslation(text, false));
                }
            };
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                if (_isInternalSend) return CallNextHookEx(_hookID, nCode, wParam, lParam);

                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Reset timer setiap kali ada ketikan baru
                _autoTranslateTimer.Stop();

                if (key == Keys.Back)
                {
                    if (_buffer.Length > 0) _buffer.Length--;
                }
                else if (key == Keys.Escape || key == Keys.Enter)
                {
                    _buffer.Clear();
                }
                else if (IsTextKey(vkCode))
                {
                    char c = ConvertToChar(vkCode);
                    if (c != '\0' && !char.IsControl(c)) 
                    {
                        _buffer.Append(c);
                        _autoTranslateTimer.Start(); // Mulai hitung mundur setelah ketik
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool IsTextKey(int vkCode)
        {
            return (vkCode >= 0x30 && vkCode <= 0x39) || // 0-9
                   (vkCode >= 0x41 && vkCode <= 0x5A) || // A-Z
                   (vkCode >= 0xBA && vkCode <= 0xC0) || // Punctuation
                   (vkCode >= 0xDB && vkCode <= 0xDE) ||
                   vkCode == 0x20; // Space
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

        private async Task ProcessTranslation(string input, bool appendSpace)
        {
            try
            {
                string translated = await TranslateText(input, SourceLang, TargetLang);
                if (translated.ToLower() == input.ToLower()) return; // Gak perlu ganti kalau sama

                _isInternalSend = true;

                // Hapus tulisan asli
                for (int i = 0; i < input.Length; i++)
                {
                    SendKey(VK_BACK);
                    await Task.Delay(5); // Kasih nafas dikit biar stabil
                }

                // Ketik hasil terjemahan
                foreach (char c in translated)
                {
                    SendUnicodeChar(c);
                    await Task.Delay(2);
                }

                if (appendSpace) SendKey(VK_SPACE);
            }
            catch { }
            finally 
            { 
                _isInternalSend = false;
                _isProcessing = false; 
            }
        }

        private void SendKey(short vk)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = vk;
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = vk;
            inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void SendUnicodeChar(char c)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = 0;
            inputs[0].u.ki.wScan = (short)c;
            inputs[0].u.ki.dwFlags = KEYEVENTF_UNICODE;
            
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = 0;
            inputs[1].u.ki.wScan = (short)c;
            inputs[1].u.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
            
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private async Task<string> TranslateText(string input, string from, string to)
        {
            // Method 1: Google Translate Primary
            string result = await TryGoogleTranslate(input, from, to);
            if (result != input) return result;

            // Method 2: Google Translate Alternate (Fallback for better accuracy)
            result = await TryGoogleTranslateAlt(input, from, to);
            
            return result;
        }

        private async Task<string> TryGoogleTranslate(string input, string from, string to)
        {
            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={HttpUtility.UrlEncode(input)}";
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    string json = await client.GetStringAsync(url);
                    var matches = Regex.Matches(json, "\"(.*?)\"");
                    if (matches.Count > 0) return matches[0].Groups[1].Value;
                }
            } catch { }
            return input;
        }

        private async Task<string> TryGoogleTranslateAlt(string input, string from, string to)
        {
            try
            {
                // Alternate endpoint often used for consistency
                string url = $"https://translate.google.com/translate_a/t?client=te&format=html&v=1.0&sl={from}&tl={to}&tk=&q={HttpUtility.UrlEncode(input)}";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    string response = await client.GetStringAsync(url);
                    // Standard response handling for this endpoint
                    if (response.StartsWith("[") && response.EndsWith("]")) return response.Trim('[', ']', '"');
                }
            } catch { }
            return input;
        }

        public void ResetBuffer()
        {
            _buffer.Clear();
            _isProcessing = false;
        }

        public void Dispose() { UnhookWindowsHookEx(_hookID); }

        #region User32.dll Imports
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);
        [DllImport("user32.dll")]
        private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        const int INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public uint dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { public int dx; public int dy; public int mouseData; public int dwFlags; public int time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT { public int uMsg; public short wParamL; public short wParamH; }
        #endregion
    }
}
