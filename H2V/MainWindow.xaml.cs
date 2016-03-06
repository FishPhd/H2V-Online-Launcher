using System;
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
    // All your variables and constants so you can easily swap this out when the servers undoubtably change
    private const string _debugFileName = "debug.log";

    private const string Url = "https://github.com/FishPhd/H2V-Online-Launcher/releases/download/0.0.0.0/";
      // Yes its ugly

    private int _fileCount; // Files that 
    private string _latestLauncherVersion; // the latest version of the launcher
    private string _latestVersion; // the latest version of h2vonline
    private bool _launcherUpdated; // Was the launcher updated during this run
    private string _localLauncherVersion; // the version of current launcher
    private string _localVersion; // version of the current h2vonline

    //Fires when the app opens (look at window_loaded event for finer tuning)
    public MainWindow()
    {
      InitiateTrace(); // Starts our console/debug trace
      try
      {
        InitializeComponent();
      }
      catch
      {
        Trace.WriteLine(@"Failed to load .Net components. Please make sure you have .Net 4.5.2 installed");
      }
      Trace.WriteLine(Cfg.Initial() ? @"Config loaded" : @"Config Defaulted");
        // Grabs our config file settings or defaults them if error
      Load(); // Loads all the settings from the cfg and sets the controls to them TODO: Databind this
      BtnAction.Content = !CheckVersion() ? "Update" : "Play";
        // If our version check doesn't go through then we use button text as handler for update
    }

    private void InitiateTrace()
    {
      File.Delete(_debugFileName); // Delete the debug.log so that we get a fresh one every launch
      Trace.WriteLine(@"Deleted " + _debugFileName);

      Trace.Listeners.Clear(); // Clear (in case called multiple times)
      //string dir = AppDomain.CurrentDomain.BaseDirectory;
      var twtl = new TextWriterTraceListener("debug.log") // Our new textwriter
      {
        Name = "TextLogger",
        TraceOutputOptions = TraceOptions.ThreadId | TraceOptions.Timestamp | TraceOptions.DateTime
      };
      var ctl = new ConsoleTraceListener(true) {TraceOutputOptions = TraceOptions.Timestamp | TraceOptions.DateTime};
      Trace.Listeners.Add(twtl);
      Trace.Listeners.Add(ctl);
      Trace.AutoFlush = true;
    }

    private void Load()
    {
      if (Cfg.ConfigFile["profile xuid 1 ="] == "0000000000000000" || Cfg.ConfigFile["profile xuid 1 ="] == "")
        // If uid is default generate a new one
      {
        UidBox.Text = GenerateUid().ToString(); //Update text box with uid from generate
        Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
        Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
      }

      PlayerName.Text = Cfg.ConfigFile["profile name 1 ="] == " " ? "" : Cfg.ConfigFile["profile name 1 ="];
        // Set textbox to blank (for watermark) TODO: Halo 2 names
      UidBox.Text = Cfg.ConfigFile["profile xuid 1 ="];
    }

    private long GenerateUid()
    {
      //Generate two ints and then append them to create our 16 digit long uid
      var rnd = new Random();
      var uid1 = rnd.Next(10000000, 100000000);
      var uid2 = rnd.Next(10000000, 100000000);
      return Convert.ToInt64($"{uid1}{uid2}"); // Return the complete uid
    }

    /*
    private bool CheckUid(long uid)
    {
      // This will check to see if the uid is taken (will need some database stuff setup)
      if (uid != null)
        return true;
      return false;
    }
    */

    private void DownloadFile(string fileUrl, string fileName, bool downloadProgress = true)
    {
      _fileCount++; // New file to download


      File.Delete(fileName); // It may or may not delete File.Delete does not throw exceptions if a file is not found
      Trace.WriteLine(@"Deleted " + fileName);

      Trace.WriteLine(@"Starting download for " + fileName + @" from " + fileUrl);

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
        Trace.WriteLine(@"Failed to download File: " + fileName);
      }
    }

    private void wc_DownloadComplete(object sender, AsyncCompletedEventArgs e)
    {
      _fileCount--; // 1 less file to download
      var filename = (string) e.UserState; // Name of file from user token in async
      Trace.WriteLine(@"Downloaded " + filename + " Files left: " + _fileCount);

      if (_fileCount == 0) // No more files to download
      {
        if (_launcherUpdated) // If the launcher was updated we need to restart
        {
          BtnAction.Content = "Restart";
          Trace.WriteLine(@"Launcher update to" + _latestLauncherVersion + "complete");
        }
        else
        {
          BtnAction.Content = "Play";
        }
        Trace.WriteLine(@"H2vonline update to" + _latestVersion + "complete");
      }
    }

    private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      DownloadBar.Value = e.ProgressPercentage; // Updates our download bar percentage
    }

    private void KillProcess(string name)
    {
      foreach (var process in Process.GetProcessesByName(name))
      {
        process.Kill(); // Kill it. Hard
        process.WaitForExit(); // Wait for process to end in case we want to do stuff with active files
      }
    }

    private bool CheckVersion()
    {
      BtnAction.Content = "Checking Version..."; // So people know what its doing

      //Grab version number from the URL constant
      var versionLines = new WebClient().DownloadString(Url + "version.txt")
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

        if (!File.Exists("MF.dll")) // If we don't find mf.dll
          DownloadFile(Url + "MF.dll", "MF.dll");

        if (!File.Exists("gungame.ini")) // If we don't find gungame.ini
          DownloadFile(Url + "gungame.ini", "gungame.ini");

        if (_localVersion != _latestVersion) // If our xlive.dll is old update it
          DownloadFile(Url + "xlive.dll", "xlive.dll");

        if (_latestLauncherVersion != _localLauncherVersion) // If our launcher is old update
        {
          DownloadFile(Url + "h2online.exe", "h2online.exe");
          _launcherUpdated = true; // Launcher was updated we will need to restart
        }

        Trace.WriteLine(@"Files Needed: " + _fileCount);
      }
      else if ((string) BtnAction.Content == "Restart") // Restart
      {
        Process.Start(Application.ResourceAssembly.Location);
        Application.Current.Shutdown();
      }
      else if ((string) BtnAction.Content == "Play")
        Process.Start("halo2.exe"); // Good to go! (may need target parameters later)
    }

    /*
    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
      if (!IsLoaded) // Don't update cfg while the control loads
        return;

      //long uid = new long(); //Here for when while loop is active

      //while (!CheckUid(uid))
      //loop here until uid is found that isn't taken
      UidBox.Text = GenerateUid().ToString(); //Update text box with uid from generate

      //Update uid value in cfg
      Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }
    */
  }
}