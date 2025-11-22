using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZeroMixUpdateCli
{
    class Program
    {
        private const string GITHUB_OWNER = "faizinuha";
        private const string GITHUB_REPO = "ZeroMix";
        private const string GITHUB_API_URL = "https://api.github.com/repos/faizinuha/ZeroMix/releases";
        private const string GITHUB_RELEASES_URL = "https://github.com/faizinuha/ZeroMix/releases";
        private const string WEBSITE_URL = "https://zeromix.vercel.app";

        private static readonly HttpClient _httpClient = new HttpClient();

        static Program()
        {
            // Set User-Agent untuk GitHub API
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ZeroMix-UpdateChecker/1.0");
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            try
            {
                // Parse arguments
                if (args.Length > 0 && args[0].Equals("cek", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > 1 && args[1].Equals("update", StringComparison.OrdinalIgnoreCase))
                    {
                        await CheckForUpdates();
                    }
                    else
                    {
                        ShowHelp();
                    }
                }
                else if (args.Length == 0 || args[0].Equals("--help", StringComparison.OrdinalIgnoreCase))
                {
                    ShowHelp();
                }
                else
                {
                    Console.WriteLine($"❌ Perintah tidak dikenali: {args[0]}");
                    ShowHelp();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine(@"
╔════════════════════════════════════════════════╗
║         ZeroMix Update Checker v1.0            ║
╚════════════════════════════════════════════════╝

Penggunaan:
  zeromix cek update     - Cek update aplikasi
  zeromix --help         - Tampilkan bantuan ini

Contoh:
  > zeromix cek update
  
");
        }

        static async Task CheckForUpdates()
        {
            Console.WriteLine("🔍 Memeriksa pembaruan...\n");

            try
            {
                // Get current version
                var currentVersion = GetCurrentVersion();
                Console.WriteLine($"📦 Versi saat ini: {currentVersion}\n");

                // Fetch releases from GitHub
                Console.WriteLine("📡 Mengambil data dari GitHub...");
                var releases = await GetGitHubReleases();

                if (releases == null || releases.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n⚠️ Tidak dapat memeriksa update. Periksa koneksi internet Anda.");
                    Console.ResetColor();
                    ShowLinks();
                    return;
                }

                // Find latest full release and pre-release
                var fullRelease = releases.FirstOrDefault(r => !r.IsPrerelease);
                var preRelease = releases.FirstOrDefault(r => r.IsPrerelease);

                if (fullRelease == null && preRelease == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n⚠️ Tidak ada release yang ditemukan.");
                    Console.ResetColor();
                    ShowLinks();
                    return;
                }

                // Determine update availability
                var latestRelease = fullRelease ?? preRelease;
                if (latestRelease != null)
                {
                    var latestVersion = NormalizeVersion(latestRelease.TagName);
                    var current = NormalizeVersion(currentVersion);

                    if (latestVersion > current)
                    {
                        DisplayUpdateAvailable(latestRelease, currentVersion);
                        await AskDownload(latestRelease);
                    }
                    else
                    {
                        DisplayNoUpdate(currentVersion);
                        ShowLinks();
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Gagal terhubung ke GitHub: {ex.Message}");
                Console.ResetColor();
                ShowLinks();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void DisplayUpdateAvailable(GitHubRelease release, string currentVersion)
        {
            Console.WriteLine("\n" + new string('─', 50));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ ADA UPDATE TERSEDIA!");
            Console.ResetColor();
            Console.WriteLine($"Versi Terbaru: {release.TagName}");
            Console.WriteLine($"Versi Saat Ini: {currentVersion}");
            Console.WriteLine($"Tipe: {(release.IsPrerelease ? "Pre-Release" : "Full Release")}");
            
            if (!string.IsNullOrWhiteSpace(release.Name))
            {
                Console.WriteLine($"Judul: {release.Name}");
            }

            // Display changelog (first 500 chars)
            if (!string.IsNullOrWhiteSpace(release.Body))
            {
                Console.WriteLine("\n📝 Changelog:");
                var changelog = release.Body.Length > 500 
                    ? release.Body.Substring(0, 500) + "...\n(selengkapnya di GitHub)" 
                    : release.Body;
                Console.WriteLine(changelog);
            }

            if (release.Assets != null && release.Assets.Count > 0)
            {
                Console.WriteLine("\n📥 Download:");
                foreach (var asset in release.Assets)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"  • {asset.Name}");
                    Console.WriteLine($"    {asset.BrowserDownloadUrl}");
                    Console.ResetColor();
                }
            }

            Console.WriteLine("\n" + new string('─', 50));
        }

        static void DisplayNoUpdate(string currentVersion)
        {
            Console.WriteLine("\n" + new string('─', 50));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✅ APLIKASI SUDAH TERBARU");
            Console.ResetColor();
            Console.WriteLine($"Versi: {currentVersion}");
            Console.WriteLine("Status: Anda menggunakan versi terbaru dari ZeroMix");
            Console.WriteLine("\n" + new string('─', 50));
        }

        static void ShowLinks()
        {
            Console.WriteLine("\n📍 Kunjungi untuk informasi lebih lanjut:");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  🌐 GitHub Releases: {GITHUB_RELEASES_URL}");
            Console.WriteLine($"  🌐 Website: {WEBSITE_URL}");
            Console.ResetColor();
        }

        static async Task AskDownload(GitHubRelease release)
        {
            Console.WriteLine("\n❓ Apakah Anda ingin membuka halaman download? (Y/N) ");
            Console.Write("> ");
            
            var response = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (response == "y" || response == "yes" || response == "ya")
            {
                try
                {
                    var url = release.HtmlUrl;
                    Console.WriteLine($"\n🌐 Membuka: {url}");
                    
                    // Open browser
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                        System.Runtime.InteropServices.OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }
                    
                    Console.WriteLine("✅ Browser sudah dibuka.");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️ Gagal membuka browser: {ex.Message}");
                    Console.WriteLine($"Kunjungi secara manual: {release.HtmlUrl}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("✓ Dibatalkan.");
            }
        }

        static async Task<List<GitHubRelease>> GetGitHubReleases()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(GITHUB_API_URL);
                using (var doc = JsonDocument.Parse(response))
                {
                    var releases = new List<GitHubRelease>();
                    
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        try
                        {
                            var release = new GitHubRelease
                            {
                                TagName = element.GetProperty("tag_name").GetString() ?? "",
                                Name = element.GetProperty("name").GetString() ?? "",
                                Body = element.GetProperty("body").GetString() ?? "",
                                IsPrerelease = element.GetProperty("prerelease").GetBoolean(),
                                HtmlUrl = element.GetProperty("html_url").GetString() ?? "",
                                Assets = new List<ReleaseAsset>()
                            };

                            // Get assets
                            if (element.TryGetProperty("assets", out var assetsElement))
                            {
                                foreach (var asset in assetsElement.EnumerateArray())
                                {
                                    release.Assets.Add(new ReleaseAsset
                                    {
                                        Name = asset.GetProperty("name").GetString() ?? "",
                                        BrowserDownloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "",
                                        Size = asset.GetProperty("size").GetInt32()
                                    });
                                }
                            }

                            releases.Add(release);
                        }
                        catch { }
                    }

                    return releases;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Debug: {ex.Message}");
                return null;
            }
        }

        static string GetCurrentVersion()
        {
            try
            {
                // Try to read from installed app registry
                var registryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ZeroMix";
                var version = Microsoft.Win32.Registry.GetValue(registryPath, "DisplayVersion", null) as string;
                
                if (!string.IsNullOrEmpty(version))
                    return version;

                // Fallback: Read from file
                var exePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "ZeroMix", "ZeroMix.exe");

                if (File.Exists(exePath))
                {
                    var version_info = FileVersionInfo.GetVersionInfo(exePath);
                    if (version_info?.FileVersion != null)
                        return version_info.FileVersion;
                }

                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        static int NormalizeVersion(string version)
        {
            // Convert "v2.1.0" to 210 for comparison
            var cleaned = version.TrimStart('v', 'V').Replace(".", "");
            if (int.TryParse(cleaned, out var result))
                return result;
            return 0;
        }
    }

    class GitHubRelease
    {
        public string TagName { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public bool IsPrerelease { get; set; }
        public string HtmlUrl { get; set; }
        public List<ReleaseAsset> Assets { get; set; } = new();
    }

    class ReleaseAsset
    {
        public string Name { get; set; }
        public string BrowserDownloadUrl { get; set; }
        public int Size { get; set; }
    }
}
