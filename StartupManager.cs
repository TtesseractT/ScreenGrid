using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace ScreenGrid
{
    /// <summary>
    /// Manages Windows startup registration via the current-user Run registry key.
    /// </summary>
    internal static class StartupManager
    {
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "ScreenGrid";

        /// <summary>Returns true if ScreenGrid is registered to run at Windows startup.</summary>
        public static bool IsRegistered()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
                return key?.GetValue(AppName) is string;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartupManager.IsRegistered error: {ex.Message}");
                return false;
            }
        }

        /// <summary>Registers the current exe to run at Windows startup.</summary>
        public static void Register()
        {
            try
            {
                string exePath = Environment.ProcessPath
                    ?? Process.GetCurrentProcess().MainModule?.FileName
                    ?? throw new InvalidOperationException("Cannot determine exe path");

                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
                    ?? throw new InvalidOperationException("Cannot open Run registry key");

                key.SetValue(AppName, $"\"{exePath}\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartupManager.Register error: {ex.Message}");
                throw;
            }
        }

        /// <summary>Removes the startup registration.</summary>
        public static void Unregister()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
                if (key?.GetValue(AppName) != null)
                    key.DeleteValue(AppName, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StartupManager.Unregister error: {ex.Message}");
                throw;
            }
        }

        /// <summary>Toggles the startup registration and returns the new state.</summary>
        public static bool Toggle()
        {
            if (IsRegistered())
            {
                Unregister();
                return false;
            }
            else
            {
                Register();
                return true;
            }
        }
    }
}
