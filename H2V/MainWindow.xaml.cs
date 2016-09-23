using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using CG.Web.MegaApiClient;
using MahApps.Metro.Controls;
using SharpCompress.Archive;
using SharpCompress.Common;
using SharpCompress.Reader;
using Application = System.Windows.Application;
using WebClient = System.Net.WebClient;

namespace h2online
{
    public partial class MainWindow
    {
        /*
            TODO: Remove the "=" from the dictionary in cfg -_-

          Sizes:
          1: 200
          2: 240
          3: 280 
            */

        #region Constants

        private const string DebugFileName = "h2vlauncher.log"; //Launcher debug output
        private const string MainWebsite = "http://www.cartographer.online/"; //Project main website
        private const string ProcessName = "halo2"; //Executable name
        private const string ProcessStartup = "startup"; //Startup splash screen
        private const string UpdateServer = "https://github.com/FishPhd/H2V-Online-Launcher/releases/download/0.0.0.0/";
        private const string LatestHalo2Version = "1.00.00.11122"; //Latest version of halo2.exe
        private const string Halo2DownloadName = "\\halo2.rar"; // The name of the halo 2 installation rar
        private const double Halo2RarSizeGb = 2.71; // Size of the Halo 2 download

        #endregion

        #region Variables

        private static readonly Dictionary<string, int> FilesDict = new Dictionary<string, int>();
        // Dictionary with files and their positions

        private int _fileCount; // # of files to download
        private string _halo2Version = "1.00.00.11122"; // Just assume everyone is up to date unless stated otherwise
        private string _latestLauncherVersion; // the latest version of the launcher
        private string _latestVersion; // the latest version of PC
        private string _localLauncherVersion; // the version of current launcher
        private string _localVersion; // local version of PC
        private readonly Timer _typingTimer = new Timer(); // timer for user typing in username
        private string _halo2DownloadUrl; // Halo 2 download redirect
        private bool _disableHalo2Download;
        public string LauncherDirectory = Environment.CurrentDirectory;
        private bool force_update = false;

        #endregion

        #region Initialize

        public MainWindow() //Fire to start app opens (look at window_loaded event for finer tuning)
        {
            InitiateTrace(); //Starts our console/debug trace
            try { InitializeComponent(); } //Try to load our pretty wpf 
            catch { Trace.WriteLine("Failed to load .Net components. Please make sure you have .Net 4.5.2 installed"); }

            Trace.WriteLine(Cfg.Initial() ? @"Config loaded" : @"Config Defaulted"); //Grabs settings or defaults them

            GetLatestVersions();
            //CheckInstall(); //Checks if game is installed properly

            if (!Load())
            {
                Cfg.DefaultSettings();
                Load();
                Trace.WriteLine("Config values failed to load. Resetting...");
            }
            else
                Trace.WriteLine("Config values loaded");

            if (Cfg.GetConfigVariable("login_token =", null) != null)
            {
                UsernameBoxLabel.Content = "Name:  ";
                Setup();
            }
            else if (UsernameBox.Text != "")
                StartTimer();
        }

        private void GetLatestVersions()
        {
            var versionLines = new WebClient().DownloadString(UpdateServer + "version.txt")
              .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None); //Grab version number from the URL constant

            // Grab latest (line 1) or dev build (line 2) 
            if (Cfg.GetConfigVariable("dev_build =", null, false) != "1")
                _latestVersion = versionLines[0];
            else
                _latestVersion = versionLines[2] + "-Beta";

            _latestLauncherVersion = versionLines[1]; //Latest launcher version from line 2 of version.txt

            //if (versionLines.Length == 3)
            //    _disableHalo2Download = true;
            //else
            //    _halo2DownloadUrl = versionLines[3]; //Halo 2 download url from line 3 of version.txt

            //Grab xlive.dll file version number. File doesn't exist? 0.0.0.0
            _localVersion = File.Exists(Cfg.InstallPath + @"\xlive.dll")
                ? FileVersionInfo.GetVersionInfo(Cfg.InstallPath + @"\xlive.dll").FileVersion
                : "0.0.0.0";
            //Grab halo2.exe file version number. 
            _halo2Version = File.Exists(Cfg.InstallPath + @"\" + ProcessName + ".exe")
                ? FileVersionInfo.GetVersionInfo(Cfg.InstallPath + @"\" + ProcessName + ".exe").FileVersion
                : "0.0.0.0";
            _localLauncherVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(); //Grab launcher version

            Trace.WriteLine("Latest h2vonline: " + _latestVersion);
            Trace.WriteLine("Latest launcher: " + _latestLauncherVersion);
            Trace.WriteLine("Local h2vonline: " + _localVersion);
            Trace.WriteLine("Local launcher: " + _localLauncherVersion);
            Trace.WriteLine("Halo 2: " + _halo2Version);
            Trace.WriteLine(Cfg.InstallPath + @"\" + ProcessName + ".exe");

            if (_localLauncherVersion != null)
            {
                LauncherVersion.Foreground = _localLauncherVersion == _latestLauncherVersion
                  ? new SolidColorBrush(Colors.Lime)
                  : new SolidColorBrush(Colors.OrangeRed);
                LauncherVersion.Content = _localLauncherVersion;
            }

            if (_localVersion != null)
            {
                PcVersion.Foreground = _localVersion == _latestVersion
                  ? new SolidColorBrush(Colors.Lime)
                  : new SolidColorBrush(Colors.OrangeRed);
                PcVersion.Content = _localVersion;
            }
        }

        private bool Load()
        //TODO: Databind this
        {
            try
            {
                UsernameBox.Text = Cfg.GetConfigVariable("name =", null);
                GameArguments.Text = Cfg.GetConfigVariable("arguments =", "");
                //CboxDebug.IsChecked = Convert.ToBoolean(Convert.ToInt32(Cfg.GetConfigVariable("debug_log =", "0")));
                Trace.WriteLine("Name: " + UsernameBox.Text); //Write Name
                //Trace.WriteLine("Debug: " + CboxDebug.IsChecked); //Write debug selection
                return true;
            }
            catch { return false; }
        }

        private void Setup()
        {
            if (Cfg.InstallPath == null)
            {
                ButtonAction.Content = "Verify";
                TextboxOutput.Text = "Please verify game installation.";
                //CheckInstall();
                //DownloadConfirmGrid.Visibility = Visibility.Visible; // Show download confirm
                //ButtonAction.Visibility = Visibility.Hidden; // Hide action button
                //TextboxOutput.Text = "Halo 2 could not be found. Locate or download?";
            }
            else
            {
                ButtonAction.Content = (!CheckVersion() ? "Update" : "Play"); //Check version and change main button depending
                TextboxOutput.Text = "Game installation verified.";
                
            }
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
            var ctl = new ConsoleTraceListener { TraceOutputOptions = TraceOptions.Timestamp };

            Trace.Listeners.Add(twtl); //Add our TWTL
            Trace.Listeners.Add(ctl); //Add our CTL
            Trace.AutoFlush = true; //Automaitcally flushes data to output on write
        }

        private bool CheckVersion()
        {
            if (File.Exists(Cfg.InstallPath + Halo2DownloadName))
                File.Delete(Cfg.InstallPath + Halo2DownloadName);

            ButtonAction.Content = "Checking Version..."; //So people know what its doing

            //If the versions are different then we update TODO: Automate this based on files on update server
            if (_localVersion != _latestVersion || _latestLauncherVersion != _localLauncherVersion ||
                _halo2Version != LatestHalo2Version || _localVersion == "0.0.0.0")
            {
                Trace.WriteLine(_halo2Version == "0.0.0.0" ? "Cannot locate halo2.exe" : "You don't have the latest version!");
                TextboxOutput.Text = "An update is available!";
                if (_halo2Version == "0.0.0.0")
                {
                    DownloadConfirmGrid.Visibility = Visibility.Visible;
                    ButtonAction.Visibility = Visibility.Hidden;
                    TextboxOutput.Text = "Halo 2 could not be found. Locate or download?";
                }
                return false;
            }
            TextboxOutput.Text = "You have the latest version!";
            Trace.WriteLine("You are up to date!");
            return true; //No update. Have fun!
        }

        private void DownloadUpdate()
        {
            // TODO: Implement a filelist.txt on server so we can grab needed files (append build number to only grab latest)
            if (_localVersion != _latestVersion)
            {
                var localZip = Cfg.InstallPath + _latestVersion + ".zip";
                DownloadFileWc(UpdateServer + _latestVersion + ".zip", localZip, true, ExtractArchive);
            }

            if (!File.Exists(Cfg.InstallPath + "h2Update.exe") && _halo2Version != LatestHalo2Version)
                DownloadFileWc(UpdateServer + "h2Update.exe", Cfg.InstallPath + "h2Update.exe");

            if (_latestLauncherVersion != _localLauncherVersion || force_update == true)
                DownloadFileWc(UpdateServer + "h2online.exe", "h2online_temp.exe");

            Trace.WriteLine("Files Needed: " + _fileCount);
        }

        private void PathFinder()
        {
            if (!IsLoaded) //Don't update cfg while the control loads
                return;

            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()) //Creates a file dialog window
            {
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); //sets default location in dialog window
                ofd.Title = "Navigate to Halo 2 Install Path"; //gives it a title
                ofd.Filter = "Halo 2 Executable|halo2.exe"; //filters out unncecessary files
                ofd.FilterIndex = 1; //allows only 1 filter index
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) //if chosen it will set the file path to the install path
                {
                    Cfg.InstallPath = ofd.FileName.Replace(ofd.SafeFileName, ""); //removes halo2.exe from file name.
                    TextboxOutput.Text = "Game installation verified."; //displays what happened
                    Cfg.SetVariable("install_path =", Cfg.InstallPath, ref Cfg.ConfigFile); //sets variable in config
                    Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile); //saves config
                    GetLatestVersions();
                    Setup();
                }
                Trace.WriteLine("Halo 2 game installation was detected."); //writes to debug file
                
            }
        }

        //private void CheckInstall()
        //{
        //  if (!IsLoaded) //Don't update cfg while the control loads
        //    return;
        //  DownloadConfirmGrid.Visibility = Visibility.Visible; // Show download confirm
        //  ButtonAction.Visibility = Visibility.Hidden; // Hide action button
        //}

        private void KillProcess(string name)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                process.Kill();
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

        public static string GetExecutingDirectoryName()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            var directoryInfo = new FileInfo(location.AbsolutePath).Directory;
            if (directoryInfo != null)
                return directoryInfo.FullName;
            Trace.WriteLine("Directory name could not be found");
            return null;
        }
        #endregion

        #region Download/File Handling

        private void DownloadFileWc(string fileUrl, string fileName, bool downloadProgress = true,
          params AsyncCompletedEventHandler[] args)
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
                    if (args != null)
                    {
                        foreach (var eventHandler in args)
                        {
                            wc.DownloadFileCompleted += eventHandler; //invoke custom handler if there is one
                        }
                    }
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
            var filename = (string)e.UserState; //Name of file from user token in async
            Trace.WriteLine("Downloaded " + filename + " File number: " + FilesDict[filename] + " Files left: " + _fileCount);
            TextboxOutput.Text = string.Empty; //Just clear textbox they will understand

            if (_fileCount == 0) //No more files to download
            {
                if (_halo2Version != LatestHalo2Version && File.Exists(Cfg.InstallPath + "h2Update.exe")) //If halo 2 is outdated, update
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
                if (FilesDict.ContainsKey("h2online_temp.exe") && _localLauncherVersion != _latestLauncherVersion || force_update == true)
                //If the launcher was updated we need to restart
                {
                    LauncherVersion.Content = _latestLauncherVersion;
                    ButtonAction.Content = "Restart";
                    Trace.WriteLine("Launcher update to " + _latestLauncherVersion + " complete.");
                    TextboxOutput.Text = "Launcher updated sucessfully! Please restart.";
                }
                else
                {
                    PcVersion.Content = _latestVersion;
                    TextboxOutput.Text = "Project Cartographer update complete!";
                    force_update = false;
                }
            }
            FilesDict.Remove(filename);
        }

        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadBar.Value = e.ProgressPercentage; // Updates our download bar percentage

            //if (e.ProgressPercentage != 100 && e.ProgressPercentage % 2 != 0)
            //    Trace.Write(e.ProgressPercentage + " ");

        }

        #endregion

        #region Events

        private void PlayerName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded || ButtonAction.Content == "Play") // Makes sure that we don't update cfg while the control loads
                return;

            StartTimer();

            Cfg.SetVariable("name =", UsernameBox.Text, ref Cfg.ConfigFile);
            Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
        }

        private void StartTimer()
        {
            if (!_typingTimer.Enabled)
            {
                _typingTimer.Interval = 500;
                _typingTimer.Enabled = true;
                _typingTimer.Tick += _typingTimer_Tick;
            }

            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void _typingTimer_Tick(object sender, EventArgs e)
        {
            if (UsernameBox.Text == string.Empty)
            {
                ButtonAction.Content = "Login";
                PasswordPanel.Visibility = Visibility.Collapsed;
                EmailPanel.Visibility = Visibility.Collapsed;
                Application.Current.MainWindow.Height = 200;
                _typingTimer.Stop();
                return;
            }

            if (Api.UsernameExists(UsernameBox.Text) == "1")
            {
                ButtonAction.Content = "Login";
                PasswordPanel.Visibility = Visibility.Visible;
                EmailPanel.Visibility = Visibility.Collapsed;
                Application.Current.MainWindow.Height = 240;
            }
            else
            {
                ButtonAction.Content = "Register";
                PasswordPanel.Visibility = Visibility.Visible;
                EmailPanel.Visibility = Visibility.Visible;
                Application.Current.MainWindow.Height = 280;
            }
            _typingTimer.Stop();
        }

        private void Icon_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(MainWebsite);
        }

        private void ExtractArchive(object sender, AsyncCompletedEventArgs e)
        {
            var localZip = Cfg.InstallPath + _latestVersion + ".zip";

            using (Stream stream = File.OpenRead(localZip))
            {
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                        reader.WriteEntryToDirectory(Cfg.InstallPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
            }

            File.Delete(localZip); // Delete archive
        }

        private void ButtonAction_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ButtonAction.Content == "Play")
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

                    DownloadConfirmGrid.Visibility = Visibility.Visible; // Show download confirm
                    ButtonAction.Visibility = Visibility.Hidden; // Hide action button
                    TextboxOutput.Text = "Halo 2 could not be found. Locate or download?";
                }
            }
            else if ((string)ButtonAction.Content == "Close Game") //Game is open
            {
                if (Cfg.CheckIfProcessIsRunning(ProcessName) || Cfg.CheckIfProcessIsRunning(ProcessStartup))
                //Might be redundant but we don't want crashes
                {
                    KillProcess(ProcessName);
                    KillProcess(ProcessStartup);
                }
                ButtonAction.Content = "Play";
            }
            else if ((string)ButtonAction.Content == "Update")
            {
                if (Cfg.CheckIfProcessIsRunning(ProcessName) || Cfg.CheckIfProcessIsRunning(ProcessStartup))
                {
                    KillProcess(ProcessName);
                    KillProcess(ProcessStartup);
                }
                    ButtonAction.Content = "Updating..."; // Button is still enabled if download is long it might look strange
                DownloadUpdate();
            }
            else if ((string)ButtonAction.Content == "Login")
            {
                // User Is logging in
                if (PasswordPanel.Visibility == Visibility.Visible)
                {
                    var loginResponse = Api.Login(UsernameBox.Text, PasswordBox.Password);
                    if (loginResponse != "0") // Correct login
                    {
                        Cfg.SetVariable("login_token =", loginResponse, ref Cfg.ConfigFile);
                        Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
                        Setup();
                    }
                    else // Incorrect login
                    {
                        TextboxOutput.Text = "Incorrect Login";
                        PasswordBox.Password = "";
                    }
                }
            }
            else if ((string)ButtonAction.Content == "Register")
            {
                if (Api.IsValidEmail(EmailBox.Text))
                {
                    var registerResponse = Api.Register(UsernameBox.Text, PasswordBox.Password, EmailBox.Text);

                    if (registerResponse != "0")
                    {
                        var loginResponse = Api.Login(UsernameBox.Text, PasswordBox.Password);

                        PasswordPanel.Visibility = Visibility.Collapsed;
                        EmailPanel.Visibility = Visibility.Collapsed;
                        Application.Current.MainWindow.Height = 200;

                        if (loginResponse != "0")
                        {
                            Cfg.SetVariable("login_token =", loginResponse, ref Cfg.ConfigFile);
                            Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
                            Setup();
                        }
                    }
                    else
                    {
                        TextboxOutput.Text = "Error Registering. Please try again shortly.";
                    }
                }
                else
                {
                    TextboxOutput.Text = "Invalid Email";
                    EmailBox.Text = "";
                }
            }
            else if ((string)ButtonAction.Content == "Verify")
            {
                PathFinder();
            }
            else if ((string)ButtonAction.Content == "Restart") // Restart
            {
                //DO NOT TOUCH THIS
                //THIS UPDATES THE LAUNCHER
                //LEAVE THIS CODE ALONE
                Task.Delay(5000);
                ProcessStartInfo file_overwrite = new ProcessStartInfo();
                file_overwrite.CreateNoWindow = true;
                file_overwrite.WindowStyle = ProcessWindowStyle.Hidden;
                file_overwrite.WorkingDirectory = LauncherDirectory;
                file_overwrite.FileName = "cmd.exe";
                file_overwrite.Arguments = "/C ping 127.0.0.1 -n 1 -w 5000 > Nul & Del h2online.exe & ping 127.0.0.1 -n 1 -w 2000 > Nul & rename h2online_temp.exe h2online.exe & ping 127.0.0.1 -n 1 -w 2000 > Nul & start h2online.exe";
                Process.Start(file_overwrite);
                Process.GetCurrentProcess().Kill();
                //DO NOT TOUCH THIS
                //THIS UPDATES THE LAUNCHER
                //LEAVE THIS CODE ALONE
            }
        }

        private void MetroWindow_Activated(object sender, EventArgs e)
        {
            if (Cfg.CheckIfProcessIsRunning(ProcessName) || Cfg.CheckIfProcessIsRunning(ProcessStartup))
                ButtonAction.Content = "Close Game";
        }

        /*
        private void CboxDebug_Changed(object sender, RoutedEventArgs e)
        {
          if (!IsLoaded) //Don't update cfg while the control loads
            return;

          // Bools convert "nicely" to 0s and 1s :)
          Cfg.SetVariable("debug =", Convert.ToString(Convert.ToInt32(CboxDebug.IsChecked)), ref Cfg.ConfigFile);
          Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
        }
        */

        private void GameArguments_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) //Don't update cfg while the control loads
                return;

            Cfg.SetVariable("arguments =", GameArguments.Text, ref Cfg.ConfigFile);
            Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile);
        }

        private void ButtonForceUpdate_Click(object sender, RoutedEventArgs e)
        {
            force_update = true;
            FlyoutHandler(LauncherSettingsFlyout); //Close flyout so user can see dl progress
            DownloadUpdate();
            Trace.WriteLine("Exe directory: " + Cfg.InstallPath);
            Trace.WriteLine("Latest Version: " + _latestVersion);
            ButtonAction.Content = "Updating...";
            if (TextboxOutput.Text == "Project Cartographer update complete!")
                ButtonAction.Content = "Restart";
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_disableHalo2Download)
            {
                TextboxOutput.Text = "Halo 2 download is currently disabled please check back later";
                return;
            }

            DownloadConfirmGrid.Visibility = Visibility.Hidden;
            CancelButton.Visibility = Visibility.Visible;
            TextboxOutput.Text = "Decrypting Url...";

            using (var folderBrowser = new FolderBrowserDialog()) //Creates a file dialog window
            {
                folderBrowser.RootFolder = Environment.SpecialFolder.Desktop; // Sets starting location in dialog window
                folderBrowser.Description = @"Select where you want to install Halo 2"; // Gives it a title
                folderBrowser.ShowNewFolderButton = true;

                if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Cfg.InstallPath = folderBrowser.SelectedPath;
                    Trace.WriteLine("Halo 2 install path selected: " + Cfg.InstallPath); //writes to debug file
                    Cfg.SetVariable("install_path =", Cfg.InstallPath, ref Cfg.ConfigFile); //sets variable in config
                    Cfg.SaveConfigFile(Cfg.InstallPath + "xlive.ini", Cfg.ConfigFile); //saves config
                }
                else
                    return;
            }

            // Downloading
            try
            {
                await MegaDownload(GetRedirectUrl(_halo2DownloadUrl));
            }
            catch
            {
                TextboxOutput.Text = "Error Downloading Halo 2. Please Restart.";
                DownloadConfirmGrid.Visibility = Visibility.Hidden;
                CancelButton.Visibility = Visibility.Hidden;
                ButtonAction.Visibility = Visibility.Visible;
                ButtonAction.Content = "Restart";
                return;
            }

            // Extracting
            var compressed = ArchiveFactory.Open(Cfg.InstallPath + Halo2DownloadName);

            TextboxOutput.Text = "Extracting...";

            foreach (var entry in compressed.Entries)
            {
                if (!entry.IsDirectory)
                {
                    entry.WriteToDirectory(Cfg.InstallPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
            }

            ButtonAction.Content = !CheckVersion() ? "Update" : "Play"; //Check version and change main button depending
            Trace.WriteLine("Halo 2 game installation complete"); //writes to debug file
            TextboxOutput.Text = "Halo 2 downloaded and installed"; //displays what happened
            DownloadConfirmGrid.Visibility = Visibility.Hidden;
            ButtonAction.Visibility = Visibility.Visible;
        }

        private async Task MegaDownload(string url)
        {
            MegaApiClient.BufferSize = 16384;
            MegaApiClient.ReportProgressChunkSize = 1200;
            var client = new MegaApiClient();
            //var stream = new CG.Web.MegaApiClient.WebClient();

            client.LoginAnonymous();

            var megaProgress = new Progress<double>(ReportProgress);

            if (File.Exists(Cfg.InstallPath + Halo2DownloadName)) // Remove old rar if found
                File.Delete(Cfg.InstallPath + Halo2DownloadName);

            await client.DownloadFileAsync(new Uri(url), Cfg.InstallPath + Halo2DownloadName, megaProgress);
        }

        private string GetRedirectUrl(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = false;
            var response = (HttpWebResponse)request.GetResponse();
            var redirUrl = response.Headers["Location"];
            response.Close();
            return redirUrl;
        }

        private void ReportProgress(double value)
        {
            var gbLeft = value / 100 * Halo2RarSizeGb;
            DownloadBar.Value = Convert.ToInt32(value);
            TextboxOutput.Text = "Downloading Halo 2: " + Math.Round(gbLeft, 2) + " gb /" + Halo2RarSizeGb + " gb (" +
                                 Math.Round(value, 2) + "%)";
        }

        private void LocateButton_Click(object sender, RoutedEventArgs e)
        {
            using (var ofd = new OpenFileDialog()) //Creates a file dialog window
            {
                ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                // Sets default location in dialog window
                ofd.Title = @"Locate Halo 2 exe"; // Gives it a title
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
            DownloadConfirmGrid.Visibility = Visibility.Hidden;
            ButtonAction.Visibility = Visibility.Visible;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        #endregion

        #region Flyouts

        private async void FlyoutHandler(Flyout sender)
        {
            await Task.Run(() => AsyncFlyoutHandler(sender));
        }

        private void AsyncFlyoutHandler(Flyout sender)
        {
            Dispatcher.Invoke(() =>
            {
                if (sender.IsOpen)
                    sender.IsOpen = false;
                else
                {
                    foreach (var fly in AllFlyouts.FindChildren<Flyout>())
                        if (fly.Header != sender.Header)
                            fly.IsOpen = false;
                    sender.IsOpen = true;
                }
            });
        }

        private void LauncherSettings_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHandler(LauncherSettingsFlyout);
        }

        #endregion
    }
}