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
        private readonly DispatcherTimer _updateTimer;

        public PluginEngine(MainWindow main)
        {
            _main = main;
            _pluginsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromSeconds(1);
            _updateTimer.Tick += (s, e) => UpdatePlugins();
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
            
            // Expose ZeroMix API to Lua
            script.Globals["ZeroMix"] = new ZeroMixLuaApi(_main);
            
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

        public void Log(string message)
        {
            Debug.WriteLine($"[LUA] {message}");
        }

        public void Notify(string title, string message)
        {
            _main.Dispatcher.Invoke(() =>
            {
                // Simple Toast using MessageBox for now, could be upgraded
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

        public void SetStatusText(string text)
        {
            _main.Dispatcher.Invoke(() => {
                _main.StatusLabel.Text = text;
            });
        }
    }
}
