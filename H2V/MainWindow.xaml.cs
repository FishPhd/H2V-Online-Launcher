using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace h2online
{
  public partial class MainWindow
  {
    // TODO: Add in dsfix or some fps limiter
    // TODO: Add in fov
    // TODO: Remove the "=" from the dictionary in cfg -_-

    // Constants
    private const string DebugFileName = "h2vlauncher.log"; // The launchers debug log output
    private const string MainWebsite = "http://www.h2v.online/"; // The project website

    // Yes its ugly
    private const string UpdateServer = "https://github.com/FishPhd/H2V-Online-Launcher/releases/download/0.0.0.0/";
    private const string LatestHalo2Version = "1.00.00.11122"; // Lateset Halo 2 vista version

    // Variables
    private static readonly Dictionary<string, int> FilesDict = new Dictionary<string, int>(); // Dictionary with files and their positions
    private int _fileCount; // # of files to download
    private string _halo2Version = "1.00.00.11122"; // Just assume everyone is up to date unless stated otherwise
    private string _latestLauncherVersion; // the latest version of the launcher
    private string _latestVersion; // the latest version of h2vonline
    private string _localLauncherVersion; // the version of current launcher
    private string _localVersion; // version of the current h2vonline

    public MainWindow() // Fire to start app opens (look at window_loaded event for finer tuning)
    {
      InitiateTrace(); // Starts our console/debug trace

      try
      {
        InitializeComponent(); // Try to load our pretty wpf
      }
      catch
      {
        Trace.WriteLine("Failed to load .Net components. Please make sure you have .Net 4.5.2 installed");
      }
      Trace.WriteLine(Cfg.Initial() ? @"Config loaded" : @"Config Defaulted"); // Grabs settings or defaults them
      if (Load())
        Trace.WriteLine("Config values loaded");
      else
      {
        Cfg.DefaultSettings();
        Load();
        Trace.WriteLine("Config values failed to load. Resetting...");
      }
      ButtonAction.Content = !CheckVersion() ? "Update" : "Play"; // Check version and change main button depending
    }

    private void InitiateTrace()
    {
      File.Delete(DebugFileName); // Delete the debug.log so that we get a fresh one every launch
      Trace.WriteLine("Created " + DebugFileName); // Our debug file was "created" c:<
      Trace.Listeners.Clear(); // Clear any listeners
      var twtl = new TextWriterTraceListener(DebugFileName) // Construct our new
      {
        Name = "DebugTrace",
        TraceOutputOptions = TraceOptions.Timestamp // use Trace.TraceXXX for timestamp on the output
      };
      var ctl = new ConsoleTraceListener {TraceOutputOptions = TraceOptions.Timestamp};
      Trace.Listeners.Add(twtl); // Add our TWTL
      Trace.Listeners.Add(ctl); // Add our CTL
      Trace.AutoFlush = true; // Automaitcally flushes data to output on write
    }

    private bool Load() // TODO: Databind this
    {
      try
      {
        // If uid is default generate a new one
        if (Cfg.ConfigFile["profile xuid 1 ="] == "0000000000000000" || Cfg.ConfigFile["profile xuid 1 ="] == "")
        {
          UidBox.Text = Auth.GenerateUid().ToString(); //Update text box with uid from generate
          Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
          Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
        }

        // Set textbox to "Player" for numbering TODO: Halo 2 names
        PlayerName.Text = Cfg.ConfigFile["profile name 1 ="] == " " ? "Player" : Cfg.ConfigFile["profile name 1 ="];

        CboxDebug.IsChecked = Convert.ToBoolean(Convert.ToInt32(Cfg.ConfigFile["debug ="]));
        UidBox.Text = Cfg.ConfigFile["profile xuid 1 ="]; // Uid box text from cfg
        Trace.WriteLine("UID: " + UidBox.Text); // Write UID
        Trace.WriteLine("Name: " + PlayerName.Text); // Write Name
        Trace.WriteLine("Debug: " + CboxDebug.IsChecked); // Write debug selection
        return true;
      }
      catch
      {
        return false;
      }
    }

    private void DownloadFile(string fileUrl, string fileName, bool downloadProgress = true)
    {
      _fileCount++; // One more file to download
      FilesDict.Add(fileName, _fileCount); // Add this file at this count to our dictionary
      Trace.WriteLine("File " + _fileCount + ": " + fileName);

      try
      {
        File.Delete(fileName); // It may or may not delete File.Delete does not throw exceptions if a file is not found
      }
      catch
      {
        Trace.WriteLine(fileName + " could not be deleted.");
      }

      TextboxOutput.Text = "Downloading: " + fileName;
      Trace.WriteLine("Starting download for " + fileName + " from " + fileUrl);

      try
      {
        using (var wc = new WebClient())
        {
          if (downloadProgress) // If item is supposed to update progressbar
            wc.DownloadProgressChanged += wc_DownloadProgressChanged; // Updates our progressbar

          wc.DownloadFileCompleted += wc_DownloadComplete; // This file has finished downloading
          wc.DownloadFileAsync(new Uri(fileUrl), fileName, fileName); // Async downloads our file overload is filename
        }
      }
      catch (Exception)
      {
        Trace.WriteLine("Failed to download File: " + fileName);
      }
    }

    private void wc_DownloadComplete(object sender, AsyncCompletedEventArgs e)
    {
      _fileCount--; // 1 less file to download
      var filename = (string) e.UserState; // Name of file from user token in async
      Trace.WriteLine("Downloaded " + filename + " File number: " + FilesDict[filename] + " Files left: " + _fileCount);
      TextboxOutput.Text = string.Empty; // Just clear textbox they will understand

      if (_fileCount == 0) // No more files to download
      {
        if (_halo2Version != LatestHalo2Version && File.Exists("h2Update.exe")) // If halo 2 is outdated update
        {
          try
          {
            Process.Start("h2Update.exe");
          }
          catch
          {
            Trace.WriteLine("Could not update Halo 2 Vista.");
          }
        }

        // If the launcher was updated we need to restart
        if (FilesDict.ContainsKey("h2online.exe") && _localVersion != _latestVersion)
        {
          ButtonAction.Content = "Restart";
          Trace.WriteLine("Launcher update to " + _latestLauncherVersion + " complete");
          Trace.WriteLine("H2vonline update to " + _latestVersion + " complete");
          TextboxOutput.Text = "Update complete! Please restart.";
        }
        else if (FilesDict.ContainsKey("h2online.exe"))
        {
          ButtonAction.Content = "Restart";
          Trace.WriteLine("Launcher update to " + _latestLauncherVersion + " complete");
          Trace.WriteLine("Please restart the launcher");
          TextboxOutput.Text = "Launcher update complete! Please restart.";
        }
        else
        {
          ButtonAction.Content = "Play";
          TextboxOutput.Text = "H2vonline update complete!";
        }
      }
    }

    private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      DownloadBar.Value = e.ProgressPercentage; // Updates our download bar percentage
      /*
      if (e.ProgressPercentage != 100 && e.ProgressPercentage % 2 != 0)
        Trace.Write(e.ProgressPercentage + " ");
        */
    }

    private void KillProcess(string name)
    {
      foreach (var process in Process.GetProcessesByName(name))
      {
        process.Kill(); // Kill it. Hard
        process.WaitForExit(); // Wait for process to end in case we want to do stuff with active files
        Trace.WriteLine(name + " process killed");
      }
    }

    private bool CheckVersion()
    {
      ButtonAction.Content = "Checking Version..."; // So people know what its doing

      //Grab version number from the URL constant
      var versionLines = new WebClient().DownloadString(UpdateServer + "version.txt")
        .Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);

      _latestVersion = versionLines[0]; // Latest h2vonline version from line 1 of version.txt
      _latestLauncherVersion = versionLines[1]; // Latest launcher version from line 2 of version.txt

      // Grab xlive.dll file version number. File doesn't exist? 0.0.0.0
      _localVersion = File.Exists(Environment.CurrentDirectory + @"\xlive.dll")
        ? FileVersionInfo.GetVersionInfo(Environment.CurrentDirectory + @"\xlive.dll").FileVersion
        : "0.0.0.0";

      // Grab halo2.exe file version number. 
      _halo2Version = File.Exists(Environment.CurrentDirectory + @"\halo2.exe")
        ? FileVersionInfo.GetVersionInfo(Environment.CurrentDirectory + @"\halo2.exe").FileVersion
        : "0.0.0.0";

      _localLauncherVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(); // Grab launcher version

      // Write all the version numbers to console
      Trace.WriteLine("Latest h2vonline version: " + _latestVersion);
      Trace.WriteLine("Latest launcher version: " + _latestLauncherVersion);
      Trace.WriteLine("Local h2vonline version: " + _localVersion);
      Trace.WriteLine("Local launcher version: " + _localLauncherVersion);
      Trace.WriteLine("Halo 2 version: " + _halo2Version);

      // If the versions are different then we update TODO: Automate this based on files on update server
      if (_localVersion != _latestVersion || _latestLauncherVersion != _localLauncherVersion || !File.Exists("MF.dll") ||
          !File.Exists("gungame.ini") || _halo2Version != LatestHalo2Version || _localVersion == "0.0.0.0")
      {
        Trace.WriteLine(_halo2Version == "0.0.0.0" ? "Cannot locate halo2.exe" : "You don't have the latest version!");
        return false;
      }
      TextboxOutput.Text = "You have the latest version!";
      Trace.WriteLine("You are up to date!");
      return true; // No update. Have fun!
    }

    private void PlayerName_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!IsLoaded) // Makes sure that we don't update cfg while the control loads
        return;

      Cfg.SetVariable("profile name 1 =", PlayerName.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
      Process.Start(MainWebsite); // Opens the website :)
    }

    private void ButtonAction_Click(object sender, RoutedEventArgs e)
    {
      if ((string)ButtonAction.Content == "Play")
      {
        try
        {
          if (Cfg.CheckIfProcessIsRunning("halo2")) // Kill halo2 if its running (Should help with black screens)
            KillProcess("halo2");
          Process.Start("halo2.exe"); // Good to go! (may need target parameters later)
        }
        catch
        {
          Trace.WriteLine("Could not locate/open Halo2.exe");
          TextboxOutput.Text = "Could not locate/open Halo2.exe";
        }
      }
      else if ((string)ButtonAction.Content == "Close Game") // Game is open
      {
        if (Cfg.CheckIfProcessIsRunning("halo2")) // Might be redundant but we don't want crashes
          KillProcess("halo2");
        ButtonAction.Content = "Play";
      }
      else if ((string) ButtonAction.Content == "Update")
      {
        KillProcess("halo2"); // Kills Halo 2 before updating TODO: add dialog before closing
        ButtonAction.Content = "Updating..."; // Button is still enabled if download is long it might look strange

        // TODO: Implement a filelist.txt on server so we can grab needed files (append build number to only grab latest)
        if (!File.Exists("MF.dll")) // If we don't find mf.dll
          DownloadFile(UpdateServer + "MF.dll", "MF.dll");

        if (!File.Exists("h2Update.exe") && _halo2Version != LatestHalo2Version) // If halo2 needs an update
          DownloadFile(UpdateServer + "h2Update.exe", "h2Update.exe");

        if (!File.Exists("gungame.ini")) // If we don't find gungame.ini
          DownloadFile(UpdateServer + "gungame.ini", "gungame.ini");

        if (_localVersion != _latestVersion) // If our xlive.dll is old
          DownloadFile(UpdateServer + "xlive.dll", "xlive.dll");

        if (_latestLauncherVersion != _localLauncherVersion) // If our launcher is old update
          DownloadFile(UpdateServer + "h2online.exe", "h2online.exe");

        //Trace.WriteLine("Files Needed: " + _fileCount);
      }
      else if ((string) ButtonAction.Content == "Restart") // Restart
      {
        Trace.WriteLine("Application restarting");
        Process.Start(Application.ResourceAssembly.Location);
        Application.Current.Shutdown();
      }
    }

    private void MetroWindow_Activated(object sender, EventArgs e)
    {
      if (Cfg.CheckIfProcessIsRunning("halo2"))// Check if halo 2 is running
        ButtonAction.Content = "Close Game";
    }

    private void CboxDebug_Changed(object sender, RoutedEventArgs e)
    {
      if (!IsLoaded) // Don't update cfg while the control loads
        return;

      // Bools convert "nicely" to 0s and 1s :)
      Cfg.SetVariable("debug =", Convert.ToString(Convert.ToInt32(CboxDebug.IsChecked)), ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }

    /*
    private void CboxHosting_Changed(object sender, RoutedEventArgs e)
    {
      if (!IsLoaded) // Don't update cfg while the control loads
        return;

      // Bools convert "nicely" to 0s and 1s :)
      Cfg.SetVariable("server =", Convert.ToString(Convert.ToInt32(CboxHosting.IsChecked)), ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }
    */
  }
}