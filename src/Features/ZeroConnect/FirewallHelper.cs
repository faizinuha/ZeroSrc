using System.Diagnostics;
using System.Security.Principal;

namespace ZeroMix.Features.ZeroConnect;

public static class FirewallHelper
{
    private const int Port = 9876;

    public static bool IsAdmin()
        => new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);

    /// <summary>
    /// Daftarkan URL ACL agar HttpListener bisa bind tanpa admin setelah ini.
    /// Hanya perlu dipanggil SEKALI (saat install / first run).
    /// </summary>
    public static async Task<bool> RegisterUrlAclAsync()
    {
        if (!IsAdmin())
        {
            // Re-launch as admin hanya untuk task ini
            return await RunElevatedAsync("--register-zeroconnect");
        }

        return await RunNetshAsync(
            $"http add urlacl url=http://+:{Port}/ user=Everyone");
    }

    public static async Task<bool> RemoveUrlAclAsync()
    {
        if (!IsAdmin()) return false;
        return await RunNetshAsync($"http delete urlacl url=http://+:{Port}/");
    }

    public static async Task<bool> AddFirewallRuleAsync()
    {
        if (!IsAdmin()) return false;

        return await RunCommandAsync("netsh", 
            $"advfirewall firewall add rule name=\"ZeroConnect\" " +
            $"dir=in action=allow protocol=TCP localport={Port}");
    }

    private static async Task<bool> RunNetshAsync(string args)
        => await RunCommandAsync("netsh", args);

    private static async Task<bool> RunCommandAsync(string cmd, string args)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        var proc = Process.Start(psi);
        if (proc == null) return false;

        await proc.WaitForExitAsync();
        return proc.ExitCode == 0;
    }

    private static async Task<bool> RunElevatedAsync(string appArgs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess().MainModule!.FileName,
            Arguments = appArgs,
            Verb = "runas", // UAC prompt
            UseShellExecute = true
        };

        try
        {
            var proc = Process.Start(psi);
            await proc!.WaitForExitAsync();
            return proc.ExitCode == 0;
        }
        catch
        {
            return false; // User cancel UAC
        }
    }
}