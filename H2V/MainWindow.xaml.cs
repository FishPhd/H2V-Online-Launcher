using System;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace H2V
{
  public partial class MainWindow
  {
    // All your variables and constants so you can easily swap this out when the servers undoubtably change
    private const string Url = "http://www.h2v.online";
    private string _latestVersion;
    private string _localVersion;

    //Fires when the app opens (look at window_loaded event for finer tuning)
    public MainWindow()
    {
      InitializeComponent();
      Cfg.Initial(); // Should check the bool it returns so it knows if the file was defaulted
      Load(); // This loads all the settings from the cfg and sets the controls to them (NEEDS DATABINDING)
      BtnAction.Content = !CheckVersion() ? "Update" : "Play";
        //If our version check doesn't go through then we use button text as handler
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
      Process.Start(Url); // Opens the website :)
    }

    private void Load()
    {
      //PlayerName.Text = Cfg.ConfigFile["profile name 1 ="] == "" ? "" : Cfg.ConfigFile["profile name 1 ="]; // If name is blank set textbox to blank (for watermark)
      //UidBox.Text = Cfg.ConfigFile["profile xuid 1 ="];
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
      if (!IsLoaded) // Makes sure that we don't update cfg while the control loads
        return;

      //long uid = new long(); //Here for when while loop is active

      //while (!CheckUid(uid))
      //loop here until uid is found that isn't taken

      UidBox.Text = GenerateUid().ToString(); //Update text box with uid from generate

      //Update uid value in cfg
      Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
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
      //This will check to see if the uid is taken (will need some database stuff setup)
      if (uid != null)
        return true;
      return false;
    }
    */

    private void DownloadFile(string fileUrl, string fileName)
    {
      using (var wc = new WebClient())
      {
        wc.DownloadProgressChanged += wc_DownloadProgressChanged; //Updates our progressbar
        wc.DownloadFileAsync(new Uri(fileUrl), fileName); //Async downloads our file
      }
    }

    private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      DownloadBar.Value = e.ProgressPercentage; // Updates our download bar percentage

      if (e.ProgressPercentage == 100)
        //If file is done downloading return button to "Play" (I think this can cause errors later on)
        BtnAction.Content = "Play";
    }

    private void BtnAction_Click(object sender, RoutedEventArgs e)
    {
      if ((string) BtnAction.Content == "Update")
      {
        KillProcess("halo2");
          // Makes sure Halo 2 isn't running before updating xlive.dll (Should put a dialog for user before killing)
        BtnAction.Content = "Updating...";
          // People can probably run partially updated game while text box is "Updating..."
        DownloadFile(Url + "/update/xlive.dll", "xlive.dll"); // Download the files needed
      }
      else
        Process.Start("halo2.exe"); // Good to go! (may need target parameters later)
    }

    private void KillProcess(string name)
    {
      var processes = Process.GetProcessesByName(name);

      foreach (var process in processes)
      {
        process.Kill(); // Kill it. Hard
        process.WaitForExit(); // Wait for process to end in case we want to do stuff with active files
      }
    }

    private bool CheckVersion()
    {
      //Grab version number from the URL constant
      _latestVersion = new WebClient().DownloadString(Url + "/update/version.txt");

      //Removes the extra linebreak (will be useful later for multiple variables in an update)
      _latestVersion.Substring(0, _latestVersion.Length);

      //Check for xlive.dll file version number
      _localVersion = FileVersionInfo.GetVersionInfo(Environment.SystemDirectory + "\\xlive.dll").FileVersion;

      if (_localVersion == _latestVersion) // If its the same we are good
        return true;
      return false; // Update time
    }

    private void PlayerName_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (!IsLoaded) // Makes sure that we don't update cfg while the control loads
        return;

      Cfg.SetVariable("profile name 1 =", PlayerName.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }
  }
}