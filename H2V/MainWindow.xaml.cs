using System;
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
    private const string Url = "https://github.com/FishPhd/H2V-Online-Launcher/releases/download/0.0.0.0/"; // Yes its ugly
    private string _latestVersion;
    private string _localLauncherVersion;
    private string _latestLauncherVersion;
    private string _localVersion;

    //Fires when the app opens (look at window_loaded event for finer tuning)
    public MainWindow()
    {
      InitializeComponent();
      Cfg.Initial(); // TODO: check bool to know if values were defaulted
      Load(); // Loads all the settings from the cfg and sets the controls to them (NEEDS DATABINDING)
      BtnAction.Content = !CheckVersion() ? "Update" : "Play"; // If our version check doesn't go through then we use button text as handler for update
    }

    private void Load()
    {
      if (Cfg.ConfigFile["profile xuid 1 ="] == "0000000000000000" || Cfg.ConfigFile["profile xuid 1 ="] == "") // If uid is default generate a new one
      {
        UidBox.Text = GenerateUid().ToString(); //Update text box with uid from generate
        Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
        Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
      }

      PlayerName.Text = Cfg.ConfigFile["profile name 1 ="] == " " ? "" : Cfg.ConfigFile["profile name 1 ="]; // If name is blank set textbox to blank (for watermark)
      UidBox.Text = Cfg.ConfigFile["profile xuid 1 ="];
      
    }

    private long GenerateUid()
    {
      //Generate two ints and then append them to create our 16 digit long uid
      Random rnd = new Random();
      int uid1 = rnd.Next(10000000, 100000000);
      int uid2 = rnd.Next(10000000, 100000000);
      return Convert.ToInt64($"{uid1}{uid2}"); // Return the complete uid
    }

    /*
    private bool CheckUid(long uid)
    {
      //This will check to see if the uid is taken (will need some database stuff setup)
      if (uid != null)
        return true;
      return false;
    }
    */

    private void DownloadFile(string fileUrl, string fileName, bool downloadProgress = true)
    {
      using (var wc = new WebClient())
      {
        if(downloadProgress)
          wc.DownloadProgressChanged += wc_DownloadProgressChanged; //Updates our progressbar
        wc.DownloadFileAsync(new Uri(fileUrl), fileName); //Async downloads our file
      }
    }

    private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      DownloadBar.Value = e.ProgressPercentage; // Updates our download bar percentage

      if (e.ProgressPercentage == 100) // File is done downloading return button to "Play" (not meant for multiple files)
        BtnAction.Content = "Play";
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
      //Grab version number from the URL constant
      string[] versionLines = new WebClient().DownloadString(Url + "version.txt").Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
      
      // Set our versions based on line position
      _latestVersion = versionLines[0];
      _latestLauncherVersion = versionLines[1];

      // Grab xlive.dll file version number. File doesn't exist? 0.0.0.0
      _localVersion = File.Exists(Environment.CurrentDirectory + @"\xlive.dll") 
        ? FileVersionInfo.GetVersionInfo(Environment.CurrentDirectory + @"\xlive.dll").FileVersion : "0.0.0.0"; 

      // Grab launcher version
      _localLauncherVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

      // If the versions are different then we update
      if (_localVersion != _latestVersion || _latestLauncherVersion != _localLauncherVersion || !File.Exists("MF.dll") || !File.Exists("gungame.ini"))
        return false;
      return true;
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
      if ((string)BtnAction.Content == "Update")
      {
        KillProcess("halo2"); // Makes sure Halo 2 isn't running before updating xlive.dll (Should put a dialog for user before killing)
        BtnAction.Content = "Updating..."; // People can probably run partially updated game while text box is "Updating..."

        if (!File.Exists("MF.dll")) // If we don't find mf.dll
          DownloadFile(Url + "MF.dll", "MF.dll", false); // Download it

        if (!File.Exists("gungame.ini")) // If we don't find gungame.ini
          DownloadFile(Url + "gungame.ini", "gungame.ini", false); // Download it

        if (_localVersion != _latestVersion) // If our xlive.dll is old update it
          DownloadFile(Url + "xlive.dll", "xlive.dll");

        if (_latestLauncherVersion != _localLauncherVersion) // If our launcher is old update and restart
        {
          DownloadFile(Url + "h2online.exe", "h2online.exe");
          Process.Start(Application.ResourceAssembly.Location);
          Application.Current.Shutdown();
        }
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