using System;

namespace h2online
{
  public class Auth
  {
    public static long GenerateUid()
    {
      //Generate two ints and then append them to create our 16 digit long uid
      var rnd = new Random();
      var uid1 = rnd.Next(10000000, 100000000);
      var uid2 = rnd.Next(10000000, 100000000);
      return Convert.ToInt64($"{uid1}{uid2}"); // Return the complete uid
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

    /*
    private bool CheckUid(long uid)
    {
      // This will check to see if the uid is taken (will need some database stuff setup)
      if (uid != null)
        return true;
      return false;
    }
    */
  }
}
