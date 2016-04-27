using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace h2online
{
    internal static class Cfg
    {
        public static Dictionary<string, string> ConfigFile = new Dictionary<string, string>();

        public static bool Initial()
        {
            var iniExists = LoadConfigFile(InstallPath + "xlive.ini", ref ConfigFile);

            if (!iniExists) // If no ini make one with these default settings
                DefaultSettings(); // Default settings

            /*
            foreach (var cfgSetting in ConfigFile) TODO: Implement this
            {
              if(!ValueExist(cfgSetting)) // Check to see if value exists
                SerVariable(dict[defaultValue]); // Grab and set value from default dict (no nulling entire cfg)
            }
            */

            if (!SaveConfigFile(InstallPath + "xlive.ini", ConfigFile)) // If cfg doesn't load return false (error)
            {
                if (File.Exists(Cfg.InstallPath + "xlive.ini"))
                    File.Delete(InstallPath + "xlive.ini"); // Pretty bad error check
                DefaultSettings(); // Default the settings
                return false; //So user can at least check if the files loaded right
            }
            return true; // All good!
        }

        //private bool ValueExist(){}
        //private void SaveValue(){}

        public static string InstallPath
        {
            get
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    if (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Halo 2\1.0\") == null)
                        Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Halo 2\1.0\");
                    return (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Halo 2\1.0\", "GameInstallDir", null);
                }
                else
                {
                    if (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft Games\Halo 2\1.0\") == null)
                        Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Microsoft Games\Halo 2\1.0\");
                    return (string)Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft Games\Halo 2\1.0\", "GameInstallDir", null);
                }
            }
            set
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    if (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Halo 2\1.0\") == null)
                        Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Halo 2\1.0\");
                    Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Microsoft Games\Halo 2\1.0\", "GameInstallDir", value);
                }
                else
                {
                    if (Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft Games\Halo 2\1.0\") == null)
                        Microsoft.Win32.Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Microsoft Games\Halo 2\1.0\");
                    Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft Games\Halo 2\1.0\", "GameInstallDir", value);
                }
            }
        }
        public static void DefaultSettings()
        {
            SetVariable("profile name 1 =", "Player", ref ConfigFile);
            SetVariable("profile xuid 1 =", "0000000000000000", ref ConfigFile);
            SetVariable("online profile =", "1", ref ConfigFile);
            SetVariable("server =", "0", ref ConfigFile);
            SetVariable("save directory =", "XLiveEmu", ref ConfigFile);
            SetVariable("debug log =", "0", ref ConfigFile);
            SetVariable("debug =", "0", ref ConfigFile);
            SetVariable("altports =", "0", ref ConfigFile);
            SetVariable("arguments =", "", ref ConfigFile);
            SetVariable("install directory =", InstallPath, ref ConfigFile);
            SaveConfigFile(InstallPath + "xlive.ini", ConfigFile);
        }

        public static void SetVariable(string varName, string varValue, ref Dictionary<string, string> configDict)
        {
            if (configDict.ContainsKey(varName))
                configDict[varName] = varValue;
            else
                configDict.Add(varName, varValue);
        }

        public static bool CheckIfProcessIsRunning(string nameSubstring)
        {
            return Process.GetProcesses().Any(p => p.ProcessName.Contains(nameSubstring));
        }

        public static bool SaveConfigFile(string cfgFileName, Dictionary<string, string> configDict)
        {
            try
            {
                var lines = configDict.Select(kvp => kvp.Key + " " + kvp.Value).ToList(); //Grab lines from the dictionary
                File.WriteAllLines(cfgFileName, lines.ToArray()); //Write all lines to xlive.ini and get out!

                return true; // Config file saved
            }
            catch
            {
                return false; // Config file not saved
            }
        }

        private static bool LoadConfigFile(string cfgFileName, ref Dictionary<string, string> returnDict)
        {
            if (returnDict == null || !File.Exists(cfgFileName)) // If cfg file doesn't exist or dict is null
                return false;

            foreach (var line in File.ReadAllLines(cfgFileName)) // For each line in the cfg
            {
                var splitIdx = line.IndexOf("=", StringComparison.Ordinal) + 1; // + 1 so we include the =

                if (splitIdx < 0 || splitIdx + 1 >= line.Length) // Makes sure the index is correct
                    continue;

                var varName = line.Substring(0, splitIdx); // 0 to the end of variable name
                var varValue = line.Substring(splitIdx + 1); // end of variable name + 1

                SetVariable(varName, varValue, ref returnDict); // Set the variable 
            }
            return true;
        }
    }
}