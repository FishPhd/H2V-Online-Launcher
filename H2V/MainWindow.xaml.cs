using System;
using System.Diagnostics;
using System.Net;
using System.Windows;

namespace H2V
{
  public partial class MainWindow
  {
    //All your variables and constants so you can easily swap this out when the servers undoubtably change
    const string Url = "http://h2v.online/update/version.txt";
    private string _latestVersion;
    private string _localVersion;

    //Fires when the app opens (look at window_loaded for finer tuning)
    public MainWindow()
    {
      InitializeComponent();
    }

    private void Icon_Click(object sender, RoutedEventArgs e)
    {
      Process.Start("http://www.h2v.online/");
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
      long uid = new long();

      //while (!CheckUid(uid))
      //loop here until uid is found that isn't taken

      //Generate two ints and then append them to create our 16 digit long uid
      Random rnd = new Random();
      int uid1 = rnd.Next(10000000, 100000000);
      int uid2 = rnd.Next(10000000, 100000000);
      uid = Convert.ToInt64($"{uid1}{uid2}");
      UidBox.Text = uid.ToString();

      //Cfg.write(uid);
      //Update uid value in cfg
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

    private void BtnAction_Click(object sender, RoutedEventArgs e)
    {
      //Checks version and responds accordingly
      Console.WriteLine(CheckVersion() ? "You are up to date!" : "You are not up to date! ");
    }

    private bool CheckVersion()
    {
      //Grab version number from the URL constant
      _latestVersion = new WebClient().DownloadString(Url);

      //Removes the extra linebreak (will be useful later for multiple variables in an update)
      _latestVersion.Substring(0, _latestVersion.Length - 1);

      //Check for xlive.dll file version number
      _localVersion = FileVersionInfo.GetVersionInfo(Environment.SystemDirectory + "\\xlive.dll").FileVersion;

      if (_localVersion == _latestVersion)
        return true;
      return false;
    }
  }
}
