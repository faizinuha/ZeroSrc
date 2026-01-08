using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using MoonSharp.Interpreter;
using System.Diagnostics;

using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Controls.Control;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;

namespace ZeroMix.Plugins
{
    public class PluginEngine
    {
        private readonly MainWindow _main;
        private readonly string _pluginsDir;
        private readonly List<LuaPlugin> _plugins = new List<LuaPlugin>();
        private readonly DispatcherTimer _updateTimer;
        private readonly FileSystemWatcher _watcher;

        public class LuaPlugin
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public bool IsEnabled { get; set; } = false;
            public Script? Script { get; set; }
            public DynamicPluginWindow? Window { get; set; }
            public ZeroMixLuaApi? Api { get; set; }
        }

        public List<LuaPlugin> GetPlugins() => _plugins;

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
                if (_plugins.Any(p => p.Path == scriptPath)) return;
                LoadPlugin(scriptPath);
            }
        }

        private void LoadPlugin(string path)
        {
            if (_plugins.Any(p => p.Path == path)) return;

            // Set context so 'require' can find files in the same folder
            string? pluginFolder = Path.GetDirectoryName(path);
            string pluginName = Path.GetFileName(pluginFolder ?? "Unknown");
            
            // By default, new plugins are DISABLED so they don't pop up immediately
            var plugin = new LuaPlugin { 
                Name = pluginName.Replace("user.pub.", "").Replace("user.priv.", ""), 
                Path = path,
                IsEnabled = false 
            };
            
            Script script = new Script();
            
            if (pluginFolder != null) {
                ((MoonSharp.Interpreter.Loaders.FileSystemScriptLoader)script.Options.ScriptLoader).ModulePaths = 
                    new string[] { Path.Combine(pluginFolder, "?.lua") };
            }

            var api = new ZeroMixLuaApi(_main, pluginFolder);
            api.SetActiveScript(script);
            plugin.Api = api;
            plugin.Script = script;

            script.Globals["CreateUI"] = (Action<string, int, int>)api.CreateUI;
            script.Globals["AddLabel"] = (Action<string>)api.AddLabel;
            script.Globals["AddInput"] = (Action<string, string>)api.AddInput;
            script.Globals["AddButton"] = (Action<string, string>)api.AddButton;
            script.Globals["GetInput"] = (Func<string, string>)api.GetInput;
            script.Globals["Notify"] = (Action<string, string>)api.Notify;
            script.Globals["SaveConfig"] = (Action<string, string>)api.SaveConfig;
            script.Globals["LoadConfig"] = (Func<string, string>)api.LoadConfig;
            script.Globals["JsonEncode"] = (Func<object, string>)api.JsonEncode;
            script.Globals["JsonDecode"] = (Func<string, object>)api.JsonDecode;
            script.Globals["Log"] = (Action<string>)api.Log;
            script.Globals["ZeroMix"] = api;
            
            // We load the script definition, but don't call OnLoad yet
            string content = File.ReadAllText(path);
            script.DoString(content);

            _plugins.Add(plugin);
            
            // Notify MainWindow to refresh the UI list
            _main.Dispatcher.BeginInvoke(new Action(() => _main.RefreshUserPluginsUI()));
        }

        public void RemovePlugin(LuaPlugin plugin)
        {
            try
            {
                plugin.Api?.CloseWindow();
                _plugins.Remove(plugin);
                
                string? folder = Path.GetDirectoryName(plugin.Path);
                if (folder != null && Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
                
                _main.RefreshUserPluginsUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Failed to delete plugin: {ex.Message}");
            }
        }

        public void TogglePlugin(LuaPlugin plugin)
        {
            plugin.IsEnabled = !plugin.IsEnabled;
            if (!plugin.IsEnabled)
            {
                plugin.Api?.CloseWindow();
            }
            else
            {
                // Re-run OnLoad to show UI if it was closed
                var onLoad = plugin.Script?.Globals["OnLoad"];
                if (onLoad != null) plugin.Script?.Call(onLoad);
            }
        }

        private void UpdatePlugins()
        {
            foreach (var plugin in _plugins)
            {
                if (!plugin.IsEnabled || plugin.Script == null) continue;
                try
                {
                    var onUpdate = plugin.Script.Globals["OnUpdate"];
                    if (onUpdate != null)
                    {
                        plugin.Script.Call(onUpdate);
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

        private readonly string? _pluginDir;

        public ZeroMixLuaApi(MainWindow main, string? pluginDir = null)
        {
            _main = main;
            _pluginDir = pluginDir;
        }

        private DynamicPluginWindow? _currentWin;
        private Dictionary<string, TextBox> _inputs = new Dictionary<string, TextBox>();
        private Script? _activeScript; // To call callbacks back

        public void SetActiveScript(Script script) => _activeScript = script;

        public void CloseWindow()
        {
            _main.Dispatcher.Invoke(() =>
            {
                _currentWin?.Close();
            });
        }

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

        public void SaveConfig(string key, string json)
        {
            if (_pluginDir == null) return;
            try
            {
                string path = Path.Combine(_pluginDir, $"{key}.json");
                File.WriteAllText(path, json);
                Log($"Saved config: {path}");
            }
            catch (Exception ex) { Log("Save Error: " + ex.Message); }
        }

        public string LoadConfig(string key)
        {
            if (_pluginDir == null) return "";
            try
            {
                string path = Path.Combine(_pluginDir, $"{key}.json");
                if (File.Exists(path)) return File.ReadAllText(path);
            }
            catch (Exception ex) { Log("Read Error: " + ex.Message); }
            return "";
        }
        public string JsonEncode(object data)
        {
            try { return JsonSerializer.Serialize(data); }
            catch { return ""; }
        }

        public object? JsonDecode(string json)
        {
            try { return JsonSerializer.Deserialize<object>(json); }
            catch { return null; }
        }
    }
}
