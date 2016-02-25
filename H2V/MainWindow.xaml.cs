using System;
using System.Diagnostics;
using System.Net;
using System.Windows;

namespace H2V
{
  public partial class MainWindow
  {
    // All your variables and constants so you can easily swap this out when the servers undoubtably change
    const string Url = "http://h2v.online/update/";
    private string _latestVersion;
    private string _localVersion;

    //Fires when the app opens (look at window_loaded for finer tuning)
    public MainWindow()
    {
      InitializeComponent();
      Cfg.Initial();
      Load(); // This loads all the settings from the cfg and sets the controls to them
      BtnAction.Content = !CheckVersion() ? "Update" : "Play"; //If our version check doesn't go through then we use button text as handler
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
      Process.Start("http://www.h2v.online/"); // Opens the website :)
    }

    private void Load()
    {
      PlayerName.Text = Cfg.ConfigFile["profile name 1 ="];
      UidBox.Text = Cfg.ConfigFile["profile xuid 1 ="];
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
      //long uid = new long(); //Here for when while loop is active

      //while (!CheckUid(uid))
      //loop here until uid is found that isn't taken

      UidBox.Text = GenerateUid().ToString(); //Update text box with uid from generate

      //Update uid value in cfg
      if (!IsLoaded) // Makes sure that we don't update cfg while the control loads
        return;

      Cfg.SetVariable("profile xuid 1 =", UidBox.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
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

    private void DownloadFile(string fileUrl, string fileName)
    {
      using (WebClient wc = new WebClient())
      {
        wc.DownloadProgressChanged += wc_DownloadProgressChanged; //Updates our progressbar
        wc.DownloadFileAsync(new Uri(fileUrl), fileName); //Async downloads our file
      }
    }

    void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      DownloadBar.Value = e.ProgressPercentage; // Updates our download bar percentage

      if (e.ProgressPercentage == 100) //If file is done downloading return button to "Play" (I think this can cause errors later on)
        BtnAction.Content = "Play";
    }

    private void BtnAction_Click(object sender, RoutedEventArgs e)
    {
      if ((string) BtnAction.Content == "Update")
      {
        KillProcess("halo2"); // Makes sure Halo 2 isn't running before updating xlive.dll (Should put a dialog for user before killing)
        BtnAction.Content = "Updating..."; // People can probably run partially updated game while text box is "Updating..."
        DownloadFile(Url + "xlive.dll", "xlive.dll"); // Download the files needed
      }   
      else
        Process.Start("halo2.exe");
    }

    private void KillProcess(string name)
    {
      Process[] processes = Process.GetProcessesByName(name);

      foreach (var process in processes)
      {
        process.Kill();
        process.WaitForExit(); // Wait for process to end in case we want to do stuff with files
      }
    }

    private bool CheckVersion()
    {
      //Grab version number from the URL constant
      _latestVersion = new WebClient().DownloadString(Url + "version.txt");

      //Removes the extra linebreak (will be useful later for multiple variables in an update)
      _latestVersion.Substring(0, _latestVersion.Length - 1);

      //Check for xlive.dll file version number
      _localVersion = FileVersionInfo.GetVersionInfo(Environment.SystemDirectory + "\\xlive.dll").FileVersion;

      if (_localVersion == _latestVersion) // If its the same we are good
        return true;
      return false; // Update time
    }

    private void PlayerName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      if (!IsLoaded) // Makes sure that we don't update cfg while the control loads
        return;

      Cfg.SetVariable("profile name 1 =", PlayerName.Text, ref Cfg.ConfigFile);
      Cfg.SaveConfigFile("xlive.ini", Cfg.ConfigFile);
    }
  }
}
