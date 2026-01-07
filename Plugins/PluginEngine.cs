using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Threading;
using MoonSharp.Interpreter;
using System.Diagnostics;

namespace ZeroMix.Plugins
{
    public class PluginEngine
    {
        private readonly MainWindow _main;
        private readonly string _pluginsDir;
        private readonly List<Script> _activeScripts = new List<Script>();
        private readonly FileSystemWatcher _watcher;

        public PluginEngine(MainWindow main)
        {
            _main = main;
            _pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += (s, e) => UpdatePlugins();

            // Setup Real-time Watcher
            _watcher = new FileSystemWatcher(_pluginsDir);
            _watcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName;
            _watcher.Created += (s, e) => {
                // Jika folder baru user.* dibuat/di-paste
                if (e.Name != null && e.Name.StartsWith("user.")) {
                    _main.Dispatcher.Invoke(() => LoadPluginFromDirectory(e.FullPath));
                    Debug.WriteLine($"[SYSTEM] New Plugin Detected & Loaded: {e.Name}");
                }
            };
            _watcher.EnableRaisingEvents = true;
        }

        public void Start()
        {
            LoadAllPlugins();
            _updateTimer.Start();
        }

        private void LoadAllPlugins()
        {
            if (!Directory.Exists(_pluginsDir)) return;

            var userPluginDirs = Directory.GetDirectories(_pluginsDir, "user.*");
            
            foreach (var dir in userPluginDirs)
            {
                string scriptPath = Path.Combine(dir, "script.lua");
                if (File.Exists(scriptPath))
                {
                    try
                    {
                        LoadPlugin(scriptPath);
                        Debug.WriteLine($"[SYSTEM] Engine: Loaded {Path.GetFileName(dir)}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ERROR] Failed to load plugin {dir}: {ex.Message}");
                    }
                }
            }
        }

        public void LoadPluginFromDirectory(string dirPath)
        {
            string scriptPath = Path.Combine(dirPath, "script.lua");
            if (File.Exists(scriptPath))
            {
                LoadPlugin(scriptPath);
            }
        }

        private void LoadPlugin(string path)
        {
            Script script = new Script();
            
            // Expose API directly to Globals for shorter calls
            var api = new ZeroMixLuaApi(_main);
            api.SetActiveScript(script);
            
            // Map common functions directly to Global scope
            script.Globals["CreateUI"] = (Action<string, int, int>)api.CreateUI;
            script.Globals["AddLabel"] = (Action<string>)api.AddLabel;
            script.Globals["AddInput"] = (Action<string, string>)api.AddInput;
            script.Globals["AddButton"] = (Action<string, string>)api.AddButton;
            script.Globals["GetInput"] = (Func<string, string>)api.GetInput;
            script.Globals["Notify"] = (Action<string, string>)api.Notify;
            script.Globals["Log"] = (Action<string>)api.Log;
            
            // Also keep the ZeroMix object for backwards compatibility
            script.Globals["ZeroMix"] = api;
            
            string content = File.ReadAllText(path);
            script.DoString(content);

            // Call OnLoad if defined
            var onLoad = script.Globals["OnLoad"];
            if (onLoad != null)
            {
                script.Call(onLoad);
            }

            _activeScripts.Add(script);
        }

        private void UpdatePlugins()
        {
            foreach (var script in _activeScripts)
            {
                try
                {
                    var onUpdate = script.Globals["OnUpdate"];
                    if (onUpdate != null)
                    {
                        script.Call(onUpdate);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SCRIPT ERROR] {ex.Message}");
                }
            }
        }
    }

    [MoonSharpUserData]
    public class ZeroMixLuaApi
    {
        private readonly MainWindow _main;

        public ZeroMixLuaApi(MainWindow main)
        {
            _main = main;
        }

        private DynamicPluginWindow? _currentWin;
        private Dictionary<string, TextBox> _inputs = new Dictionary<string, TextBox>();
        private Script? _activeScript; // To call callbacks back

        public void SetActiveScript(Script script) => _activeScript = script;

        public void CreateUI(string title, int width, int height)
        {
            _main.Dispatcher.Invoke(() =>
            {
                _currentWin = new DynamicPluginWindow();
                _currentWin.TitleText.Text = title;
                _currentWin.Width = width;
                _currentWin.Height = height;
                _currentWin.Show();
                _inputs.Clear();
            });
        }

        public void AddLabel(string text)
        {
            _main.Dispatcher.Invoke(() =>
            {
                var label = new TextBlock { Text = text, Foreground = new SolidColorBrush(Color.FromRgb(87, 96, 111)), FontSize = 12, FontWeight = FontWeights.SemiBold };
                _currentWin?.AddControl(label);
            });
        }

        public void AddInput(string id, string placeholder)
        {
            _main.Dispatcher.Invoke(() =>
            {
                var input = new TextBox { 
                    Tag = id, 
                    Text = placeholder, 
                    Padding = new Thickness(10), 
                    Background = new SolidColorBrush(Color.FromRgb(249, 249, 249)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                    BorderThickness = new Thickness(1)
                };
                _inputs[id] = input;
                _currentWin?.AddControl(input);
            });
        }

        public void AddButton(string text, string callbackName)
        {
            _main.Dispatcher.Invoke(() =>
            {
                var btn = new Button { 
                    Content = text, 
                    Padding = new Thickness(20, 10, 20, 10),
                    Background = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                
                btn.Click += (s, e) => {
                    if (_activeScript != null) {
                        var func = _activeScript.Globals[callbackName];
                        if (func != null) _activeScript.Call(func);
                    }
                };

                _currentWin?.AddControl(btn);
            });
        }

        public string GetInput(string id)
        {
            string val = "";
            _main.Dispatcher.Invoke(() => {
                if (_inputs.ContainsKey(id)) val = _inputs[id].Text;
            });
            return val;
        }

        public void Log(string message)
        {
            Debug.WriteLine($"[LUA] {message}");
        }

        public void Notify(string title, string message)
        {
            _main.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            });
        }

        public double GetCpuUsage()
        {
            double val = 0;
            _main.Dispatcher.Invoke(() => {
                if (double.TryParse(_main.CpuPercentText.Text.Replace(" %", ""), out double result))
                    val = result;
            });
            return val;
        }

        public double GetRamUsage()
        {
            double val = 0;
            _main.Dispatcher.Invoke(() => {
                if (double.TryParse(_main.RamPercentText.Text.Replace(" %", ""), out double result))
                    val = result;
            });
            return val;
        }

        public int GetTimeHour() => DateTime.Now.Hour;
        public int GetTimeMin() => DateTime.Now.Minute;

        public void SetStatusText(string text)
        {
            _main.Dispatcher.Invoke(() => {
                _main.StatusLabel.Text = text;
            });
        }
    }
}
