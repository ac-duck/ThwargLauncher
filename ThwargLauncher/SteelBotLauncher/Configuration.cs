using System;
using System.IO;

namespace SteelBotLauncher
{
    class Configuration
    {
        public static string AppFolder
        {
            get
            {
                return SteelFilter.FileLocations.AppFolder;
            }
        }
        public static string OldAppFolder
        {
            get
            {
                return SteelFilter.FileLocations.OldAppFolder;
            }
        }
        public static string UserPreferencesFile
        {
            get
            {
                string mydocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(mydocs, "Asheron's Call\\UserPreferences.ini");
            }
        }
        public static string UserPreferencesBaseFile
        {
            get
            {
                string mydocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                return Path.Combine(mydocs, "Asheron's Call\\UserPreferences_base.ini");
            }
        }
    }
}
