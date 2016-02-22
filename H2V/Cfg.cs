using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace H2V
{
  internal static class Cfg
  {
    public static Dictionary<string, string> IniFile = new Dictionary<string, string>();

    public static void Initial(string error)
    {
      var iniExists = LoadConfigFile("xlive.ini", ref IniFile);

      if (!iniExists)
      {
        SetVariable("profile name 1 =", "", ref IniFile);
        SetVariable("profile xuid 1 =", "0000000000000000", ref IniFile);
      }

      if (!SaveConfigFile("xlive.ini", IniFile))
        Console.WriteLine(@"Failed to save xlive.ini");
    }

    public static void SetVariable(string varName, string varValue, ref Dictionary<string, string> configDict)
    {
      if (configDict.ContainsKey(varName))
        configDict[varName] = varValue;
      else
        configDict.Add(varName, varValue);
    }

    public static bool SaveConfigFile(string cfgFileName, Dictionary<string, string> configDict)
    {
      try
      {
        //Grab lines from the dictionary
        var lines = configDict.Select(kvp => kvp.Key + " \"" + kvp.Value + "\"").ToList();

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
          continue; // Line isn't valid?
        var varName = line.Substring(0, splitIdx);
        var varValue = line.Substring(splitIdx + 1);

        // Remove quotes
        if (varValue.StartsWith("\""))
          varValue = varValue.Substring(1);
        if (varValue.EndsWith("\""))
          varValue = varValue.Substring(0, varValue.Length - 1);

        SetVariable(varName, varValue, ref returnDict);
      }
      return true;
    }
  }
}