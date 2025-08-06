using Microsoft.Win32;
using System.Diagnostics;

namespace DdcTraySwitcher
{
    internal static class AutoStart
    {
        private const string RunKey = @"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "DdcTraySwitcher";

        public static void Register()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            var path = Process.GetCurrentProcess().MainModule.FileName;
            key?.SetValue(AppName, path);
        }

        public static void Unregister()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(AppName, false);
        }

        public static bool IsRegistered()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            var path = Process.GetCurrentProcess().MainModule.FileName;
            var current = key?.GetValue(AppName) as string;
            return current == path;
        }
    }
}
