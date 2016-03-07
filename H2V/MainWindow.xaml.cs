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
    // Constants
    private const string DebugFileName = "h2vlauncher.log"; // The launchers debug log output
    private const string Url = "http://www.h2v.online/";
    private const string UpdateServer = "https://github.com/FishPhd/H2V-Online-Launcher/releases/download/0.0.0.0/"; // Yes its ugly

    // Variables
    private static readonly Dictionary<string, int> FilesDict = new Dictionary<string, int>(); // Dictionary with files and their positions
    private int _fileCount; // # of files to download
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

      // Grabs our config file settings or defaults them if error
      Trace.WriteLine(Cfg.Initial() ? @"Config loaded" : @"Config Defaulted");
      Load(); // Loads all the settings from the cfg and sets the controls to them 
      BtnAction.Content = !CheckVersion() ? "Update" : "Play"; // Check version and change main button depending
    }

    private void InitiateTrace()
    {
      File.Delete(DebugFileName); // Delete the debug.log so that we get a fresh one every launch
      Trace.WriteLine("Created " + DebugFileName); // Our debug file was "created" c:<
      Trace.Listeners.Clear(); // Clear any listeners

      TextWriterTraceListener twtl = new TextWriterTraceListener(DebugFileName) // Construct our new
      {
        Name = "DebugTrace",
        TraceOutputOptions = TraceOptions.Timestamp // use Trace.TraceXXX for timestamp on the output
      };
      ConsoleTraceListener ctl = new ConsoleTraceListener { TraceOutputOptions = TraceOptions.Timestamp };
      Trace.Listeners.Add(twtl); // Add our TWTL
      Trace.Listeners.Add(ctl); // Add our CTL
      Trace.AutoFlush = true; // Automaitcally flushes data to output on write
    }

    private void Load() // TODO: Databind this
    {
      // If uid is default generate a new one
      if (Cfg.ConfigFile["profile xuid 1 ="] == "0000000000000000" || Cfg.ConfigFile["profile xuid 1 ="] == "")
      {
        UidBox.Text = Auth.GenerateUid().ToString(); //Update text box with uid from generate
        Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
        Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
      }

      // Set textbox to blank (for watermark) TODO: Halo 2 names
      PlayerName.Text = Cfg.ConfigFile["profile name 1 ="] == " " ? "" : Cfg.ConfigFile["profile name 1 ="];
      UidBox.Text = Cfg.ConfigFile["profile xuid 1 ="]; // Uid box text from cfg
      Trace.WriteLine("UID: " + UidBox.Text); // Write UID
      Trace.WriteLine("Name: " + PlayerName.Text); // Write Name
    }

    private void DownloadFile(string fileUrl, string fileName, bool downloadProgress = true)
    {
      _fileCount++; // One more file to download
      FilesDict.Add(fileName, _fileCount); // Add this file at this count to our dictionary
      Trace.WriteLine("File "+ _fileCount + ": " + fileName);

      File.Delete(fileName); // It may or may not delete File.Delete does not throw exceptions if a file is not found
      //Trace.WriteLine("Deleted " + fileName);

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
      string filename = (string) e.UserState; // Name of file from user token in async
      Trace.WriteLine("Downloaded " + filename + " File number: " + FilesDict[filename] + " Files left: " + _fileCount);

      if (_fileCount == 0) // No more files to download
      {
        if (FilesDict.ContainsKey("h2online.exe") && _localVersion != _latestVersion) // If the launcher was updated we need to restart
        {
          BtnAction.Content = "Restart";
          Trace.WriteLine("Launcher update to " + _latestLauncherVersion + " complete");
          Trace.WriteLine("H2vonline update to " + _latestVersion + " complete");
        }
        else if (FilesDict.ContainsKey("h2online.exe"))
        {
          BtnAction.Content = "Restart";
          Trace.WriteLine("Launcher update to " + _latestLauncherVersion + " complete");
          Trace.WriteLine("Please restart the launcher");
        }
        else
          BtnAction.Content = "Play";
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
      BtnAction.Content = "Checking Version..."; // So people know what its doing

      //Grab version number from the URL constant
      string[] versionLines = new WebClient().DownloadString(UpdateServer + "version.txt")
        .Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);

      _latestVersion = versionLines[0]; // Latest h2vonline version from line 1 of version.txt
      _latestLauncherVersion = versionLines[1]; // Latest launcher version from line 2 of version.txt


      _localVersion = File.Exists(Environment.CurrentDirectory + @"\xlive.dll")
        // Grab xlive.dll file version number. File doesn't exist? 0.0.0.0
        ? FileVersionInfo.GetVersionInfo(Environment.CurrentDirectory + @"\xlive.dll").FileVersion
        : "0.0.0.0";

      _localLauncherVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(); // Grab launcher version

      // Write all the version numbers to console
      Trace.WriteLine("Latest h2vonline version: " + _latestVersion + Environment.NewLine + "Latest launcher version: " +
                      _latestLauncherVersion + Environment.NewLine + "Local h2vonline version: " + _localVersion +
                      Environment.NewLine + "Local launcher version: " + _localLauncherVersion);

      // If the versions are different then we update
      if (_localVersion != _latestVersion || _latestLauncherVersion != _localLauncherVersion || !File.Exists("MF.dll") ||
          !File.Exists("gungame.ini"))
      {
        Trace.WriteLine("You don't have the latest version!");
        return false;
      }
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
      Process.Start(Url); // Opens the website :)
    }

    private void BtnAction_Click(object sender, RoutedEventArgs e)
    {
      if ((string) BtnAction.Content == "Update")
      {
        KillProcess("halo2"); // Kills Halo 2 before updating TODO: add dialog before closing
        BtnAction.Content = "Updating..."; // Button is still enabled if download is long it might look strange

        // TODO: Implement a filelist.txt on server so we can grab needed files (append build number to only grab latest)
        if (!File.Exists("MF.dll")) // If we don't find mf.dll
          DownloadFile(UpdateServer + "MF.dll", "MF.dll");

        /* TODO: Implement latest Halo 2 update patch probably with exe version check
        if (!File.Exists("halo2update.exe")) // If we don't find mf.dll
          DownloadFile(UpdateServer + "halo2update.exe", "halo2update.exe");
          */

        if (!File.Exists("gungame.ini")) // If we don't find gungame.ini
          DownloadFile(UpdateServer + "gungame.ini", "gungame.ini");

        if (_localVersion != _latestVersion) // If our xlive.dll is old update it
          DownloadFile(UpdateServer + "xlive.dll", "xlive.dll");

        if (_latestLauncherVersion != _localLauncherVersion) // If our launcher is old update
          DownloadFile(UpdateServer + "h2online.exe", "h2online.exe");

        //Trace.WriteLine("Files Needed: " + _fileCount);
      }
      else if ((string) BtnAction.Content == "Restart") // Restart
      {
        Trace.WriteLine("Application restarting");
        Process.Start(Application.ResourceAssembly.Location);
        Application.Current.Shutdown();
      }
      else if ((string) BtnAction.Content == "Play")
        Process.Start("halo2.exe"); // Good to go! (may need target parameters later)
    }

    private void CboxHosting_Changed(object sender, RoutedEventArgs e)
    {
      if (!IsLoaded) // Don't update cfg while the control loads
        return;

      // Bools convert "nicely" to 0s and 1s :)
      Cfg.SetVariable("server =", Convert.ToString(Convert.ToInt32(CboxHosting.IsChecked)), ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }

    private void CboxDebug_Changed(object sender, RoutedEventArgs e)
    {
      if (!IsLoaded) // Don't update cfg while the control loads
        return;

      // Bools convert "nicely" to 0s and 1s :)
      Cfg.SetVariable("debug =", Convert.ToString(Convert.ToInt32(CboxDebug.IsChecked)), ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }
  }
}