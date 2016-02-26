using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace H2V
{
  internal static class Cfg
  {
    public static Dictionary<string, string> ConfigFile = new Dictionary<string, string>();

    public static bool Initial()
    {
      var iniExists = LoadConfigFile("xlive.ini", ref ConfigFile);

      if (!iniExists) // If no ini make one with these default settings
        DefaultSettings(); // Default settings


      /*
      foreach (var cfgSetting in ConfigFile)
      {
        // Check to see if value exists
        if(!ValueExist(cfgSetting))
          SerVariable(dict[defaultValue]); // Grab the default value for that setting and set that value from dictionary (so we aren't nulling the entire cfg)
      }

      private bool ValueExist()
      {

      }
      */

      if (!SaveConfigFile("xlive.ini", ConfigFile)) // If cfg doesn't load return false (error)
      {
        // Pretty bad error check
        File.Delete("xlive.ini");
        DefaultSettings();
        return false; //So user can at least check if the files loaded right (doesn't do anything atm)
      }
      return true;
    }

    public static void DefaultSettings()
    {
      SetVariable("profile name 1 =", "", ref ConfigFile);
      SetVariable("profile xuid 1 =", "0000000000000000", ref ConfigFile);
      SetVariable("online profile =", "1", ref ConfigFile);
      SetVariable("server =", "0", ref ConfigFile);
      SetVariable("save directory =", "XLiveEmu", ref ConfigFile);
      SetVariable("debug log =", "1", ref ConfigFile);
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
        //Grab lines from the dictionary
        var lines = configDict.Select(kvp => kvp.Key + " " + kvp.Value).ToList();
        //var lines = configDict.Select(kvp => kvp.Key + " \"" + kvp.Value + "\"").ToList();

        //Write all lines to xlive.ini and get out!
        File.WriteAllLines(cfgFileName, lines.ToArray());
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static bool LoadConfigFile(string cfgFileName, ref Dictionary<string, string> returnDict)
    {
      if (returnDict == null) throw new ArgumentNullException(nameof(returnDict));

      if (!File.Exists(cfgFileName))
        return false;

      var lines = File.ReadAllLines(cfgFileName);
      foreach (var line in lines)
      {
        var splitIdx = line.IndexOf(" ", StringComparison.Ordinal);
        if (splitIdx < 0 || splitIdx + 1 >= line.Length)
          continue;
        var varName = line.Substring(0, splitIdx);
        var varValue = line.Substring(splitIdx + 1);

        /*
        // Remove quotes
        if (varValue.StartsWith("\""))
          varValue = varValue.Substring(1);
        if (varValue.EndsWith("\""))
          varValue = varValue.Substring(0, varValue.Length - 1);
          */
        SetVariable(varName, varValue, ref returnDict);
      }
      return true;
    }
  }
}