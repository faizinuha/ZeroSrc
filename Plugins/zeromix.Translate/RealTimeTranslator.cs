using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Input;
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
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        private StringBuilder _buffer = new StringBuilder();
        public string SourceLang { get; set; } = "id";
        public string TargetLang { get; set; } = "en";

        private bool _isProcessing = false;

        public RealTimeTranslator()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
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
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && !_isProcessing)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // TRIGGER: CTRL + SPACE
                bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                if (key == Keys.Space && ctrl)
                {
                    string text = _buffer.ToString().Trim();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _isProcessing = true;
                        _buffer.Clear();
                        Task.Run(() => ProcessTranslation(text));
                    }
                    return (IntPtr)1; 
                }

                // Buffer Logic
                if (key == Keys.Back)
                {
                    if (_buffer.Length > 0) _buffer.Length--;
                }
                else if (key == Keys.Enter || key == Keys.Escape)
                {
                    _buffer.Clear();
                }
                else
                {
                    char c = ConvertToChar(vkCode);
                    if (c != '\0') _buffer.Append(c);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private char ConvertToChar(int vkCode)
        {
            byte[] keyState = new byte[256];
            GetKeyboardState(keyState);
            
            // Perbaiki deteksi Shift untuk huruf kapital dan simbol
            StringBuilder sb = new StringBuilder(2);
            uint scanCode = MapVirtualKey((uint)vkCode, 0);
            int result = ToUnicode((uint)vkCode, scanCode, keyState, sb, sb.Capacity, 0);

            if (result > 0) return sb[0];
            return '\0';
        }

        private async Task ProcessTranslation(string input)
        {
            try
            {
                string translated = await TranslateText(input, SourceLang, TargetLang);
                
                // Hapus teks lama dengan backspace presisi
                for (int i = 0; i < input.Length; i++)
                {
                    SendKeys.SendWait("{BACKSPACE}");
                }

                // "Ketik" hasil terjemahan (Handle spesial karakter SendKeys)
                string safeText = translated.Replace("{", "{{").Replace("}", "}}").Replace("(", "{(}").Replace(")", "{)}").Replace("+", "{+}").Replace("^", "{^}").Replace("%", "{%}");
                SendKeys.SendWait(safeText);
            }
            catch { }
            finally { _isProcessing = false; }
        }

        private async Task<string> TranslateText(string input, string from, string to)
        {
            try
            {
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={from}&tl={to}&dt=t&q={HttpUtility.UrlEncode(input)}";
                using (HttpClient client = new HttpClient())
                {
                    string json = await client.GetStringAsync(url);
                    var match = Regex.Match(json, "\"(.*?)\"");
                    if (match.Success) return match.Groups[1].Value;
                }
            } catch { }
            return input;
        }

        public void Dispose() { UnhookWindowsHookEx(_hookID); }

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
    }
}
