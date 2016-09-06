using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace h2online
{
  class Api
  {
    private const string ApiUrl = "http://cartographer.online/api.php"; // Api url for login and registration

    public static string Register(string user, string pass, string email)
    {
      using (var client = new WebClient())
      {
        var values = new NameValueCollection();
        values["launcher"] = "1";
        values["user"] = user;
        values["pass"] = pass;
        values["email"] = email;

        var response = client.UploadValues(ApiUrl, values);

        return Encoding.Default.GetString(response);
      }
      /*
      using (var client = new HttpClient())
      {
        var values = new Dictionary<string, string>
        {
          {"launcher", "1"},
          {"user", user},
          {"pass", pass},
          {"email",  email}
        };

        var content = new FormUrlEncodedContent(values);
        var response = await client.PostAsync("http://cartographer.online/api.php", content);

        var responseString = await response.Content.ReadAsStringAsync();

        return responseString;
      }
      */
    }

    public static string UsernameExists(string user)
    {
      using (var client = new WebClient())
      {
        var values = new NameValueCollection();
        values["launcher"] = "1";
        values["user"] = user;

        var response = client.UploadValues(ApiUrl, values);

        return Encoding.Default.GetString(response);
      }

      /*
      using (var client = new HttpClient())
      {
        var values = new Dictionary<string, string>
        {
          {"launcher", "1"},
          {"user", user}
        };

        var content = new FormUrlEncodedContent(values);
        var response = await client.PostAsync("http://cartographer.online/api.php", content);

        var responseString = await response.Content.ReadAsStringAsync();

        return responseString;
      }
      */
    }

    public static string Login(string user, string pass, string token = null, bool usingToken = false)
    {
      using (var client = new WebClient())
      {
        var values = new NameValueCollection();
        values["launcher"] = "1";

        if (usingToken)
        {
          values["token"] = token;
        }
        else
        {
          values["user"] = user;
          values["pass"] = pass;
        }

        var response = client.UploadValues(ApiUrl, values);
        var responseString = Encoding.Default.GetString(response);
        Console.WriteLine(responseString);
        return responseString;
      }

      /*
      using (var client = new HttpClient())
      {
        Dictionary<string, string> values;

        if(usingToken)
        {
          values = new Dictionary<string, string>
          {
            {"launcher", "1"},
            {"token", token}
          };
        }
        else
        {
          values = new Dictionary<string, string>
          {
            {"launcher", "1"},
            {"user", user},
            {"pass", pass}
          };
        }

        var content = new FormUrlEncodedContent(values);

        var response = await client.PostAsync("http://cartographer.online/api.php", content);

        var responseString = await response.Content.ReadAsStringAsync();

        return responseString;
      }
      */
    }
  }
}
