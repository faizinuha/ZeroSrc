using System;
using System.IO;
using System.Windows;
using System.Text.Json;

namespace ZeroMix
{
    public partial class App : System.Windows.Application
    {
        public static HotkeyCore? HotkeyCoreInstance { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool isFirstRun = true;

            if (File.Exists("config.json"))
            {
                
                try
                {
                    string jsonString = File.ReadAllText("config.json");
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        if (doc.RootElement.TryGetProperty("IsFirstRun", out JsonElement element))
                        {
                            isFirstRun = element.GetBoolean();
                        }
                    }
                }
                catch
                {
                    // Error reading config or invalid JSON, treat as first run
                    isFirstRun = true;
                }
            }

            if (isFirstRun)
            {
                var onboarding = new ZeroMix.Onboarding.OnboardingWindow();
                onboarding.OnOnboardingFinished += StartMainApp;
                onboarding.Show();
            }
            else
            {
                StartMainApp();
            }
        }

        private void StartMainApp()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();

            HotkeyCoreInstance = new HotkeyCore();
            HotkeyCoreInstance.Show();
        }
    }
}
