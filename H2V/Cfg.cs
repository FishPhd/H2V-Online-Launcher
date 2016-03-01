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
      bool iniExists = LoadConfigFile("xlive.ini", ref ConfigFile);

      if (!iniExists) // If no ini make one with these default settings
        DefaultSettings(); // Default settings

      /*
      foreach (var cfgSetting in ConfigFile) TODO: Implement this
      {
        if(!ValueExist(cfgSetting)) // Check to see if value exists
          SerVariable(dict[defaultValue]); // Grab and set value from default dict (no nulling entire cfg)
      }
      */

      if (!SaveConfigFile("xlive.ini", ConfigFile)) // If cfg doesn't load return false (error)
      {
        File.Delete("xlive.ini");// Pretty bad error check
        DefaultSettings(); // Default the settings
        return false; //So user can at least check if the files loaded right (doesn't do anything atm)
      }
      return true; // All good!
    }

    //private bool ValueExist(){}
    //private void SaveValue(){}

    public static void DefaultSettings()
    {
      SetVariable("profile name 1 =", " ", ref ConfigFile);
      SetVariable("profile xuid 1 =", "0000000000000000", ref ConfigFile);
      SetVariable("online profile =", "1", ref ConfigFile);
      SetVariable("server =", "0", ref ConfigFile);
      SetVariable("save directory =", "XLiveEmu", ref ConfigFile);
      SetVariable("debug log =", "1", ref ConfigFile);
      SetVariable("altports =", "0", ref ConfigFile);
      SaveConfigFile("xlive.ini", ConfigFile);
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

        return true;
      }
      catch
      {
        return false;
      }
    }

    private static bool LoadConfigFile(string cfgFileName, ref Dictionary<string, string> returnDict)
    {
      if (returnDict == null || !File.Exists(cfgFileName)) // If cfg file doesn't exist or dict is null
        return false;

      foreach (string line in File.ReadAllLines(cfgFileName)) // For each line in the cfg
      {
        int splitIdx = line.IndexOf("=", StringComparison.Ordinal) + 1; // + 1 so we include the =

        if (splitIdx < 0 || splitIdx + 1 >= line.Length) // Makes sure the index is correct
          continue;

        string varName = line.Substring(0, splitIdx); // 0 to the end of variable name
        string varValue = line.Substring(splitIdx + 1); // end of variable name + 1

        SetVariable(varName, varValue, ref returnDict); // Set the variable 
      }
      return true;
    }
  }
}