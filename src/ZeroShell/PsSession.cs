using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroMix.ZeroShell
{
    /// <summary>
    /// Manages a PowerShell (pwsh.exe) process with:
    /// - Full stdin/stdout/stderr piping
    /// - PWD tracking via prompt marker parsing
    /// - Automatic crash recovery
    /// - Terminal resize notification
    /// - UTF-8 encoding throughout
    /// </summary>
    public class PsSession : IDisposable
    {
        private Process? _process;
        private StreamWriter? _stdin;
        private bool _isDisposed;
    private bool _isRestarting;
    private readonly string _shellExe;

        // Prompt detection: \n[ZS:PWD=<fullpath>]> 
        private static readonly Regex PwdRegex = new(
            @"\n\[ZS:PWD=(.+?)\]> ",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private string _currentDirectory;
        private readonly string _startDirectory;

        // Internal buffer for incomplete prompt patterns
        private string _leftover = "";

        // ── Events ──────────────────────────────────────────────
        /// <summary>Raised on the reader thread when stdout data arrives.</summary>
        public event Action<string>? OutputData;

        /// <summary>Raised on the reader thread when stderr data arrives.</summary>
        public event Action<string>? ErrorData;

        /// <summary>Raised when the current directory changes (detected via prompt).</summary>
        public event Action<string>? DirectoryChanged;

        /// <summary>Raised when the process exits unexpectedly.</summary>
        public event Action<string>? ProcessTerminated;

        /// <summary>Raised after a successful restart.</summary>
        public event Action? ProcessRestarted;

        // ── Properties ──────────────────────────────────────────
        public string CurrentDirectory => _currentDirectory;
        public bool IsRunning => _process != null && !_process.HasExited;
        public int? ExitCode => _process?.HasExited == true ? _process?.ExitCode : null;
        public string ShellExe => _shellExe;

        // ── Constructor ─────────────────────────────────────────
        public PsSession(string? workingDirectory = null)
        {
            _startDirectory = workingDirectory
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _currentDirectory = _startDirectory;
            _shellExe = ResolveShell();
        }

        // ── Start / Stop ────────────────────────────────────────

        public void Start()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(PsSession));

            string initScript = BuildInitScript();
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(initScript));

            _process = new Process();
            _process.StartInfo = new ProcessStartInfo
            {
                FileName = _shellExe,
                Arguments = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -NoExit -EncodedCommand {encoded}",
                WorkingDirectory = _startDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            _process.EnableRaisingEvents = true;
            _process.Exited += OnExited;

            try
            {
                _process.Start();
            }
            catch (Exception ex)
            {
                _process = null;
                throw new InvalidOperationException(
                    $"Failed to start {_shellExe}. Is PowerShell installed?", ex);
            }

            _stdin = _process.StandardInput;
            _stdin.AutoFlush = true;

            // Read streams on background threads
            Task.Run(() => ReadStreamAsync(_process.StandardOutput, isError: false));
            Task.Run(() => ReadStreamAsync(_process.StandardError, isError: true));
        }

        public void Stop()
        {
            try
            {
                if (_stdin != null)
                {
                    lock (_stdin) { try { _stdin.WriteLine("exit"); } catch { } }
                }

                if (_process != null && !_process.HasExited)
                {
                    if (!_process.WaitForExit(3000))
                    {
                        _process.Kill(entireProcessTree: true);
                        _process.WaitForExit(1000);
                    }
                }
            }
            catch { }
            finally
            {
                _process = null;
                _stdin = null;
            }
        }

        public void Restart()
        {
            if (_isRestarting) return;
            _isRestarting = true;

            try
            {
                try { Stop(); } catch { }
                Start();
            }
            finally
            {
                _isRestarting = false;
            }

            ProcessRestarted?.Invoke();
        }

        // ── Command Execution ───────────────────────────────────

        /// <summary>
        /// Send a command/line to the shell's stdin.
        /// Safe to call from any thread.
        /// </summary>
        public Task ExecuteAsync(string command)
        {
            if (_stdin == null)
                throw new InvalidOperationException("Session is not started.");

            if (_process == null || _process.HasExited)
                throw new InvalidOperationException("Process has exited. Call Restart() first.");

            lock (_stdin)
            {
                _stdin.WriteLine(command);
            }

            return Task.CompletedTask;
        }

        // ── Resize ──────────────────────────────────────────────

        /// <summary>
        /// Notify the shell process that the terminal dimensions changed.
        /// PowerShell supports $Host.UI.RawUI.BufferSize / WindowSize.
        /// For a non-Console process we send a resize command via stdin.
        /// </summary>
        public void Resize(int columns, int rows)
        {
            if (!IsRunning) return;

            columns = Math.Clamp(columns, 20, 500);
            rows = Math.Clamp(rows, 5, 200);

            // PowerShell script to resize its virtual console buffer
            string resizeCmd =
                $"[Console]::BufferWidth = $Host.UI.RawUI.BufferSize.Width = {columns}; " +
                $"[Console]::WindowHeight = $Host.UI.RawUI.WindowSize.Height = {rows}; " +
                $"[Console]::WindowWidth = $Host.UI.RawUI.WindowSize.Width = {Math.Min(columns, 120)}";

            try { ExecuteAsync(resizeCmd); } catch { }
        }

        // ── Stream Reading ──────────────────────────────────────

        private async Task ReadStreamAsync(StreamReader reader, bool isError)
        {
            char[] buf = new char[4096];
            try
            {
                while (!reader.EndOfStream && !_isDisposed)
                {
                    int n = await reader.ReadAsync(buf, 0, buf.Length);
                    if (n <= 0) break;

                    string chunk = new string(buf, 0, n);

                    if (isError)
                    {
                        // Only stdout has PWD markers
                        ErrorData?.Invoke(chunk);
                    }
                    else
                    {
                        // Parse stdout for PWD tracking
                        string processed = DetectPromptAndExtractPwd(chunk);
                        OutputData?.Invoke(processed);
                    }
                }
            }
            catch (Exception ex) when (!_isDisposed && !_isRestarting)
            {
                ProcessTerminated?.Invoke($"Read error: {ex.Message}");
            }
        }

        /// <summary>
        /// Scans output for the prompt marker [ZS:PWD=...] and extracts the current directory.
        /// The marker itself is stripped from the output before forwarding.
        /// </summary>
        private string DetectPromptAndExtractPwd(string chunk)
        {
            // Prepend leftover from last call
            string data = _leftover + chunk;
            _leftover = "";

            var match = PwdRegex.Match(data);
            if (!match.Success)
            {
                // Maybe the marker is split across chunks — keep trailing data if it looks
                // like it could be the start of a marker.
                int lastNl = data.LastIndexOf('\n');
                if (lastNl >= 0 && data.Length - lastNl > 4 && data[lastNl] == '\n')
                {
                    // Only buffer if the tail starts with the marker prefix
                    string tail = data.Substring(lastNl);
                    if (tail.Contains("[ZS"))
                    {
                        _leftover = tail;
                        return data.Substring(0, lastNl);
                    }
                }
                return data;
            }

            string newDir = match.Groups[1].Value;

            if (!string.Equals(_currentDirectory, newDir, StringComparison.OrdinalIgnoreCase))
            {
                _currentDirectory = newDir;
                DirectoryChanged?.Invoke(newDir);
            }

            // Strip the marker from output so it doesn't show in the terminal
            string cleaned = PwdRegex.Replace(data, "\n> ");
            return cleaned;
        }

        // ── Process Exit ────────────────────────────────────────

        private void OnExited(object? sender, EventArgs e)
        {
            if (_isDisposed || _isRestarting) return;

            int? code = _process?.HasExited == true ? _process?.ExitCode : null;
            ProcessTerminated?.Invoke($"Process exited{(code.HasValue ? $" (code {code})" : "")}.");

            // Auto-restart after 500ms (with safety wrap)
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                if (!_isDisposed)
                {
                    try { Restart(); }
                    catch { /* suppressed — restart will be retried on next command attempt */ }
                }
            });
        }

        // ── Shell Resolution ────────────────────────────────────

        private static string ResolveShell()
        {
            // Try pwsh.exe first (cross-platform PowerShell Core)
            if (TryFindShell("pwsh.exe")) return "pwsh.exe";
            // Fall back to Windows PowerShell
            if (TryFindShell("powershell.exe")) return "powershell.exe";
            // Last resort — let the OS resolve via PATH
            return "pwsh.exe";
        }

        private static bool TryFindShell(string name)
        {
            try
            {
                using var test = new Process
                {
                    StartInfo = new ProcessStartInfo(name, "-NoLogo -NoProfile -Command \"$true\"")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                };
                test.Start();
                return test.WaitForExit(1000) && test.ExitCode == 0;
            }
            catch { return false; }
        }

        // ── Init Script ─────────────────────────────────────────

        private static string BuildInitScript()
        {
            // Critical: UTF-8 everywhere, custom prompt with PWD marker, clean startup
            return @"
$OutputEncoding = [System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.PrivateData.ErrorForegroundColor = 'Red'
$Host.PrivateData.WarningForegroundColor = 'Yellow'
$Host.PrivateData.DebugForegroundColor = 'Cyan'
$Host.PrivateData.VerboseForegroundColor = 'Green'
$MaximumHistoryCount = 9999

function prompt {
    $p = (Get-Location).Path
    return ""`n[ZS:PWD=$p]> ""
}

Clear-Host
";
        }

        // ── IDisposable ─────────────────────────────────────────

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            if (_process != null)
                _process.Exited -= OnExited;

            Stop();
        }
    }
}
