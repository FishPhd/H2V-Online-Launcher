using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;

namespace h2online
{
  internal class Api
  {
    private const string ApiUrl = "http://cartographer.online/api.php"; // Api url for login and registration

    public static string Register(string user, string pass, string email)
    {
      var pairs = new List<KeyValuePair<string, string>>
      {
        new KeyValuePair<string, string>("launcher", "1"),
        new KeyValuePair<string, string>("user", user),
        new KeyValuePair<string, string>("pass", pass),
        new KeyValuePair<string, string>("email", email)
      };

      var content = new FormUrlEncodedContent(pairs);

      using (var client = new HttpClient())
      {
        var response = client.PostAsync(ApiUrl, content).Result;
        var contentString = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine(contentString);
        return contentString;
      }
    }

    public static string UsernameExists(string user)
    {
      var pairs = new List<KeyValuePair<string, string>>
      {
        new KeyValuePair<string, string>("launcher", "1"),
        new KeyValuePair<string, string>("user", user)
      };

      var content = new FormUrlEncodedContent(pairs);

      using (var client = new HttpClient())
      {
        var response = client.PostAsync(ApiUrl, content).Result;
        var contentString = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine(contentString);
        return contentString;
      }
    }

    public static string Login(string user, string pass, string token = null)
    {
      var pairs = new List<KeyValuePair<string, string>>
      {
        new KeyValuePair<string, string>("launcher", "1")
      };

      if (token != null)
        pairs.Add(new KeyValuePair<string, string>("token", token));
      else
      {
        pairs.Add(new KeyValuePair<string, string>("user", user));
        pairs.Add(new KeyValuePair<string, string>("pass", pass));
      }

      var content = new FormUrlEncodedContent(pairs);

      using (var client = new HttpClient())
      {
        var response = client.PostAsync(ApiUrl, content).Result;
        var contentString = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine(contentString);
        return contentString;
      }
    }

    public static bool IsValidEmail(string email)
    {
      try
      {
        var addr = new MailAddress(email);
        return addr.Address == email;
      }
      catch
      {
        return false;
      }
    }
  }
}