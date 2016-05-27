using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace h2online
{
  public partial class MainWindow
  {
    /*
        TODO: Remove the "=" from the dictionary in cfg -_-
        */

    #region Constants

    private const string DebugFileName = "h2vlauncher.log"; //Launcher debug output
    private const string CurrentExeName = "h2online.exe"; //Launcher debug output
    private const string MainWebsite = "http://www.h2v.online/"; //Project main website
    private const string ProcessName = "halo2"; //Executable name
    private const string ProcessStartup = "startup"; //Startup splash screen
    private const string UpdateServer = "https://github.com/FishPhd/H2V-Online-Launcher/releases/download/0.0.0.0/";
    private const string LatestHalo2Version = "1.00.00.11122"; //Latest version of halo2.exe

    #endregion

    #region Variables

    private static readonly Dictionary<string, int> FilesDict = new Dictionary<string, int>();
    // Dictionary with files and their positions

    private int _fileCount; // # of files to download
    private string _halo2Version = "1.00.00.11122"; // Just assume everyone is up to date unless stated otherwise
    private string _latestLauncherVersion; // the latest version of the launcher
    private string _latestVersion; // the latest version of h2vonline
    private string _localLauncherVersion; // the version of current launcher
    private string _localVersion; // version of the current h2vonline

    #endregion

    #region initialize

    public MainWindow() //Fire to start app opens (look at window_loaded event for finer tuning)
    {
      InitiateTrace(); //Starts our console/debug trace
      CheckInstall(); //Checks if game is installed properly
      try
      {
        InitializeComponent(); //Try to load our pretty wpf
      }
      catch
      {
        Trace.WriteLine("Failed to load .Net components. Please make sure you have .Net 4.5.2 installed");
      }
      Trace.WriteLine(Cfg.Initial() ? @"Config loaded" : @"Config Defaulted"); //Grabs settings or defaults them

      if (Load())
        Trace.WriteLine("Config values loaded");
      else
      {
        Cfg.DefaultSettings();
        Load();
        Trace.WriteLine("Config values failed to load. Resetting...");
      }
      if (Cfg.InstallPath == null)
        ButtonAction.Content = "Verify Install"; //Check if install path exists, and changes verify button
      else
        ButtonAction.Content = !CheckVersion() ? "Update" : "Play"; //Check version and change main button depending
    }

    private void InitiateTrace()
    {
      if (File.Exists(Cfg.InstallPath + DebugFileName)) //Checks if file exists before attempting to delete
        File.Delete(Cfg.InstallPath + DebugFileName); //Delete the debug.log so that we get a fresh one every launch

      Trace.WriteLine("Created " + DebugFileName); //Our debug file was "created" c:<
      Trace.Listeners.Clear(); //Clear any listeners

      var twtl = new TextWriterTraceListener(Cfg.InstallPath + DebugFileName) //Construct our new
      {
        Name = "DebugTrace",
        TraceOutputOptions = TraceOptions.Timestamp //use Trace.TraceXXX for timestamp on the output
      };
      var ctl = new ConsoleTraceListener {TraceOutputOptions = TraceOptions.Timestamp};

      Trace.Listeners.Add(twtl); //Add our TWTL
      Trace.Listeners.Add(ctl); //Add our CTL
      Trace.AutoFlush = true; //Automaitcally flushes data to output on write
    }

    private bool CheckVersion()
    {
      ButtonAction.Content = "Checking Version..."; //So people know what its doing
      var versionLines = new WebClient().DownloadString(UpdateServer + "version.txt")
        .Split(new[] {"\r\n", "\n"}, StringSplitOptions.None); //Grab version number from the URL constant

      _latestVersion = versionLines[0]; //Latest h2vonline version from line 1 of version.txt
      _latestLauncherVersion = versionLines[1]; //Latest launcher version from line 2 of version.txt
      _localVersion = File.Exists(Cfg.InstallPath + @"\xlive.dll")
        ? FileVersionInfo.GetVersionInfo(Cfg.InstallPath + @"\xlive.dll").FileVersion
        : "0.0.0.0"; //Grab xlive.dll file version number. File doesn't exist? 0.0.0.0
      _halo2Version = File.Exists(Cfg.InstallPath + @"\" + ProcessName + ".exe")
        ? FileVersionInfo.GetVersionInfo(Cfg.InstallPath + @"\" + ProcessName + ".exe").FileVersion
        : "0.0.0.0"; //Grab halo2.exe file version number. 
      _localLauncherVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(); //Grab launcher version

      Trace.WriteLine("Latest h2vonline version: " + _latestVersion);
      Trace.WriteLine("Latest launcher version: " + _latestLauncherVersion);
      Trace.WriteLine("Local h2vonline version: " + _localVersion);
      Trace.WriteLine("Local launcher version: " + _localLauncherVersion);
      Trace.WriteLine("Halo 2 version: " + _halo2Version);

      //If the versions are different then we update TODO: Automate this based on files on update server
      if (_localVersion != _latestVersion || _latestLauncherVersion != _localLauncherVersion ||
          !File.Exists(Cfg.InstallPath + "MF.dll") ||
          !File.Exists(Cfg.InstallPath + "gungame.ini") ||
          _halo2Version != LatestHalo2Version ||
          _localVersion == "0.0.0.0")
      {
        Trace.WriteLine(_halo2Version == "0.0.0.0" ? "Cannot locate halo2.exe" : "You don't have the latest version!");
        return false;
      }
      
      TextboxOutput.Text = "You have the latest version!";
      Trace.WriteLine("You are up to date!");
      return true; //No update. Have fun!
    }

    //TODO: Databind this
    private bool Load()
    {
      try
      {
        //If uid is default generate a new one
        if (Cfg.ConfigFile["profile xuid 1 ="] == "0000000000000000" || Cfg.ConfigFile["profile xuid 1 ="] == "")
        {
          UidBox.Text = Auth.GenerateUid(); // Update text box with uid from generate
          Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
          Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
        }
        PlayerName.Text = Cfg.ConfigFile["profile name 1 ="] == " "
          ? "Player"
          : Cfg.GetConfigVariable("profile name 1 =", "Player");
        //Set textbox to "Player" for numbering TODO: Halo 2 names
        GameArguments.Text = Cfg.GetConfigVariable("arguments =", " ");
        //Set textbox to load saved command parameters
        CboxDebug.IsChecked = Convert.ToBoolean(Convert.ToInt32(Cfg.GetConfigVariable("debug =", "0")));
        UidBox.Text = Cfg.GetConfigVariable("profile xuid 1 =", "0000000000000000"); //Uid box text from cfg
        Trace.WriteLine("UID: " + UidBox.Text); //Write UID
        Trace.WriteLine("Name: " + PlayerName.Text); //Write Name
        Trace.WriteLine("Debug: " + CboxDebug.IsChecked); //Write debug selection
        return true;
      }
      catch
      {
        return false;
      }
    }

    private void CheckInstall()
    {
      if (!IsLoaded) //Don't update cfg while the control loads
        return;

      using (var ofd = new OpenFileDialog()) //Creates a file dialog window
      {
        ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        // Sets default location in dialog window
        ofd.Title = @"Navigate to Halo 2 Install Path"; // Gives it a title
        ofd.Filter = @"Halo 2 Executable|halo2.exe"; // Filters out unncecessary files
        ofd.FilterIndex = 1; // Allows only 1 filter index

        //Ff chosen it will set the file path to the install path
        if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
          if (ofd.SafeFileName != null)
            Cfg.InstallPath = ofd.FileName.Replace(ofd.SafeFileName, ""); //removes halo2.exe from file name.
        }
      }

      Cfg.SetVariable("install directory =", Cfg.InstallPath, ref Cfg.ConfigFile); //sets variable in config
      Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile); //saves config
      ButtonAction.Content = !CheckVersion() ? "Update" : "Play"; //Check version and change main button depending
      Trace.WriteLine("Halo 2 game installation was detected."); //writes to debug file
      TextboxOutput.Text = "Game installation verified."; //displays what happened
    }

    #endregion

    #region Download

    private void DownloadFile(string fileUrl, string fileName, bool downloadProgress = true)
    {
      _fileCount++; //One more file to download
      FilesDict.Add(fileName, _fileCount); //Add this file at this count to our dictionary
      Trace.WriteLine("File " + _fileCount + ": " + fileName);
      try
      {
        File.Delete(fileName); //It may or may not delete File.Delete does not throw exceptions if a file is not found  
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
          if (downloadProgress) //If item is supposed to update progressbar
            wc.DownloadProgressChanged += wc_DownloadProgressChanged; //Updates our progressbar
          wc.DownloadFileCompleted += wc_DownloadComplete; //This file has finished downloading
          wc.DownloadFileAsync(new Uri(fileUrl), fileName, fileName); //Async downloads our file overload is filename
        }
      }
      catch (Exception)
      {
        Trace.WriteLine("Failed to download File: " + fileName);
      }
    }

    private void wc_DownloadComplete(object sender, AsyncCompletedEventArgs e)
    {
      _fileCount--; //1 less file to download
      var filename = (string) e.UserState; //Name of file from user token in async
      Trace.WriteLine("Downloaded " + filename + " File number: " + FilesDict[filename] + " Files left: " + _fileCount);
      TextboxOutput.Text = string.Empty; //Just clear textbox they will understand

      if (_fileCount == 0) //No more files to download
      {
        if (_halo2Version != LatestHalo2Version && File.Exists("h2Update.exe")) //If halo 2 is outdated, update
        {
          try
          {
            Process.Start(Cfg.InstallPath + "h2Update.exe");
          }
          catch
          {
            Trace.WriteLine("Could not update Halo 2 Vista.");
          }
        }
        if (FilesDict.ContainsKey("h2online.exe") && _localVersion != _latestVersion)
          //If the launcher was updated we need to restart
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

    #endregion

    #region Events

    private void KillProcess(string name)
    {
      foreach (var process in Process.GetProcessesByName(name))
      {
        process.Kill(); //Kill it. Hard
        process.WaitForExit(); //Wait for process to end in case we want to do stuff with active files
        Trace.WriteLine(name + " process killed");
      }
    }

    private void StartProcess(string name)
    {
      if (name == null) throw new ArgumentNullException(nameof(name));
      var psi = new ProcessStartInfo
      {
        WorkingDirectory = Cfg.InstallPath, //Process install path
        FileName = ProcessName + ".exe", //process start name
        Arguments = GameArguments.Text //Process command parameters
      };
      Process.Start(psi); // Start process
    }

    private void PlayerName_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!IsLoaded) // Makes sure that we don't update cfg while the control loads
        return;
      Cfg.SetVariable("profile name 1 =", PlayerName.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
      Process.Start(MainWebsite); //Opens the website :)
    }

    private void ButtonAction_Click(object sender, RoutedEventArgs e)
    {
      if ((string) ButtonAction.Content == "Play")
      {
        try
        {
          if (Cfg.CheckIfProcessIsRunning(ProcessName) || Cfg.CheckIfProcessIsRunning(ProcessStartup))
            //Kill halo2 or startup if its running (Should help with black screens)
          {
            KillProcess(ProcessName);
            KillProcess(ProcessStartup);
          }
          StartProcess(ProcessStartup);
        }
        catch
        {
          Trace.WriteLine("Could not find or launch Halo 2 application.");
          TextboxOutput.Text = "Could not find or launch Halo 2 application.";
          CheckInstall(); //will run install check 
          ButtonAction.Content = "Verify Install";
        }
      }
      else if ((string) ButtonAction.Content == "Verify Install")
      {
        CheckInstall();
      }
      else if ((string) ButtonAction.Content == "Close Game") //Game is open
      {
        if (Cfg.CheckIfProcessIsRunning(ProcessName) || Cfg.CheckIfProcessIsRunning(ProcessStartup))
          //Might be redundant but we don't want crashes
        {
          KillProcess(ProcessName);
          KillProcess(ProcessStartup);
        }
        ButtonAction.Content = "Play";
      }
      else if ((string) ButtonAction.Content == "Update")
      {
        var tmp = Environment.CurrentDirectory; //gets current directory of launcher
        KillProcess(ProcessName); // Kills Halo 2 before updating TODO: add dialog before closing
        ButtonAction.Content = "Updating..."; // Button is still enabled if download is long it might look strange

        // TODO: Implement a filelist.txt on server so we can grab needed files (append build number to only grab latest)
        if (!File.Exists(Cfg.InstallPath + "MF.dll")) // If we don't find mf.dll
          DownloadFile(UpdateServer + "MF.dll", Cfg.InstallPath + "MF.dll");

        if (!File.Exists(Cfg.InstallPath + "h2Update.exe") && _halo2Version != LatestHalo2Version)
          // If halo2 needs an update
          DownloadFile(UpdateServer + "h2Update.exe", Cfg.InstallPath + "h2Update.exe");

        if (!File.Exists(Cfg.InstallPath + "gungame.ini")) // If we don't find gungame.ini
          DownloadFile(UpdateServer + "gungame.ini", Cfg.InstallPath + "gungame.ini");

        if (_localVersion != _latestVersion) // If our xlive.dll is old
          DownloadFile(UpdateServer + "xlive.dll", Cfg.InstallPath + "xlive.dll");

        if (_latestLauncherVersion != _localLauncherVersion) // If our launcher is old update
          DownloadFile(UpdateServer + "h2online.exe", tmp + "/" + "h2online.exe");

        Trace.WriteLine("Files Needed: " + _fileCount);
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
      if (Cfg.CheckIfProcessIsRunning(ProcessName) || Cfg.CheckIfProcessIsRunning(ProcessStartup))
        // Check if halo 2 is running
        ButtonAction.Content = "Close Game";
    }

    private void CboxDebug_Changed(object sender, RoutedEventArgs e)
    {
      if (!IsLoaded) //Don't update cfg while the control loads
        return;

      // Bools convert "nicely" to 0s and 1s :)
      Cfg.SetVariable("debug =", Convert.ToString(Convert.ToInt32(CboxDebug.IsChecked)), ref Cfg.ConfigFile);
      Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
    }

    private void GameArguments_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!IsLoaded) //Don't update cfg wh ile the control loads
        return;

      Cfg.SetVariable("arguments =", GameArguments.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
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

    #endregion
  }
}