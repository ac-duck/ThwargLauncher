using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace SteelBotLauncher
{
    class DecalInjection
    {
        public static string GetDecalLocation()
        {
            string subKey = "SOFTWARE\\Decal\\Agent";
            try
            {
                RegistryKey sk1 = Registry.LocalMachine.OpenSubKey(subKey);
                if (sk1 == null) { throw new Exception("Decal registry key not found: " + subKey); }

                string decalInjectionFile = (string)sk1.GetValue("AgentPath", "");
                if (string.IsNullOrEmpty(decalInjectionFile)) { throw new Exception("Decal AgentPath"); }

                decalInjectionFile += "Inject.dll";

                if (decalInjectionFile.Length > 5 && File.Exists(decalInjectionFile))
                {
                    return decalInjectionFile;
                }
            }
            catch (Exception exc)
            {
                throw new Exception("No Decal in registry: " + exc.Message);
            }
            return "NoDecal";
        }

        public static bool IsDecalInstalled()
        {
            string subKey = @"SOFTWARE\Decal\Agent";
            try
            {
                RegistryKey sk1 = Registry.LocalMachine.OpenSubKey(subKey);
                if (sk1 == null) { return false; }
                string decalInjectionFile = (string)sk1.GetValue("AgentPath", "");
                if (string.IsNullOrEmpty(decalInjectionFile)) { return false; }
                decalInjectionFile += "Inject.dll";

                if (!File.Exists(decalInjectionFile)) { return false; }

                return true;
            }
            catch (Exception exc)
            {
                throw new Exception("No Decal in registry: " + exc.Message);
            }
        }
        public static bool IsSteelFilterRegistered()
        {
            string subKey = @"SOFTWARE\Decal\NetworkFilters\{EF403F22-65F5-4DC2-B34D-6318E5489E8E}";
            try
            {
                RegistryKey sk1 = Registry.LocalMachine.OpenSubKey(subKey);
                if (sk1 == null) { return false; }
                string SteelFilterDLL = (string)sk1.GetValue("Path", "");
                if (string.IsNullOrEmpty(SteelFilterDLL)) { return false; }
                SteelFilterDLL += @"\SteelFilter.dll";

                if (!File.Exists(SteelFilterDLL)) { return false; }

                return true;
            }
            catch (Exception exc)
            {
                throw new Exception("SteelFilter is not configured in decal." + exc.Message);
            }
        }
        public static bool IsSteelFilterEnabled()
        {
            string subKey = @"SOFTWARE\Decal\NetworkFilters\{EF403F22-65F5-4DC2-B34D-6318E5489E8E}";
            try
            {
                RegistryKey sk1 = Registry.LocalMachine.OpenSubKey(subKey);
                if (sk1 == null) { return false; }
                var SteelFilterEnabled = (int)sk1.GetValue("Enabled", 0);
                if ((SteelFilterEnabled != 1)) { return false; }

                return true;
            }
            catch (Exception exc)
            {
                throw new Exception("SteelFilter is not enabled in decal." + exc.Message);
            }
        }
    }
}
