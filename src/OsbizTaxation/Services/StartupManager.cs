using System;
using Microsoft.Win32;

namespace OsbizTaxation.Services
{
    /// <summary>
    /// Gere le demarrage automatique de l'application avec Windows
    /// via la cle de registre Run de l'utilisateur courant (HKCU).
    /// </summary>
    public static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "OsbizTaxation";

        /// <summary>Indique si le demarrage automatique est actuellement active.</summary>
        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
                return key?.GetValue(ValueName) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Active ou desactive le demarrage automatique.</summary>
        public static void SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
                                ?? Registry.CurrentUser.CreateSubKey(RunKey);
                if (key == null) return;

                if (enabled)
                    key.SetValue(ValueName, BuildCommand(), RegistryValueKind.String);
                else if (key.GetValue(ValueName) != null)
                    key.DeleteValue(ValueName, false);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Reecrit la commande si le demarrage est active (utile apres une
        /// mise a jour, pour pointer vers le nouvel emplacement de l'exe).
        /// </summary>
        public static void Sync(bool enabled)
        {
            if (enabled || IsEnabled())
                SetEnabled(enabled);
        }

        private static string BuildCommand()
        {
            var exe = Environment.ProcessPath ?? "";
            return $"\"{exe}\" --minimized";
        }
    }
}
