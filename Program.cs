using Newtonsoft.Json;
using System.Net;
using IWshRuntimeLibrary;
using static Create_Steam_Shortcuts.steam_API_JSON;
using ImageMagick;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace Create_Steam_Shortcuts
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SteamResponse respon = GetGameData();

            if (!Directory.Exists("tmp")) Directory.CreateDirectory("tmp");
            Console.WriteLine($"==========\nApp Name\n==========");
            Parallel.ForEach(respon.apps, (game) =>
            {
                string gameName = SanitizeFileName(game.name);
                Console.WriteLine(gameName);
                string icon = $"tmp\\{game.appid}.ico";
                DownloadImage(game, icon);
                AddShortcut($"steam://rungameid/{game.appid}", gameName, icon);
            });
            Console.WriteLine("\nFinish...");
            Console.Read();
        }

        private static void DownloadImage(SteamApp game, string destination)
        {
            string imgHash = game.icon;
            string imageURL = $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{game.appid}/{imgHash}.jpg";
            string image = $"tmp\\{game.appid}.jpg";

            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] dataArr = client.DownloadData(imageURL);
                    System.IO.File.WriteAllBytes(image, dataArr);
                    ConvertImageToIcon(image, destination);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static SteamResponse GetGameData()
        {

            string token = JsonConvert.DeserializeObject<steamToken>(System.IO.File.ReadAllText("token.json")).token;

            string apiURL = $"https://api.steampowered.com/ICommunityService/GetApps/v1/?access_token={token}";
            int i = 0;
            foreach (string id in getAppID())
            {
                apiURL += $"&appids%5B{i++}%5D={id}";
            }
            return JsonConvert.DeserializeObject<Root>(getWebContent(apiURL)).response;
        }

        private static List<string> getAppID()
        {
            try
            {
                List<string> list = new List<string>();
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                if (key != null)
                {
                    string vdfLoc = Path.Combine(key.GetValue("InstallPath").ToString(), "steamapps", "libraryfolders.vdf");
                    string vdf = System.IO.File.ReadAllText(vdfLoc);
                    Regex regex = new Regex("\"apps\"\n\t\t{[^}]*\\}", RegexOptions.IgnoreCase);
                    MatchCollection apps = regex.Matches(vdf);
                    Console.WriteLine($"==========\nFound App ID\n==========");
                    foreach(Match app in apps.ToArray())
                    {
                        string values = app.Value.Replace("\t","").Replace("\"apps\"\n{\n","");
                        if (values.Length - 2 <= 0) continue;
                        values = values.Substring(0, values.Length - 2);

                        foreach(string value in values.Split('\n'))
                        {
                            string appID = value.Substring(1, value.Substring(1).IndexOf('\"'));
                            Console.WriteLine(appID);
                            list.Add(appID);
                        }
                    }
                    key.Close();
                    return list;
                }
                throw new Exception("Error locating steam library");
            }
            catch
            {
                Console.WriteLine("Error locating steam library");
            }
            return null;
        }

        private static void AddShortcut(string pathTo, string shortcutName, string icon)
        {
            string programs_path = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string shortcutFolder = Path.Combine(programs_path, @"SteamAppShortcuts");
            if (!Directory.Exists(shortcutFolder))
            {
                Directory.CreateDirectory(shortcutFolder);
            }
            WshShell shell = new WshShell();
            string settingsLink = Path.Combine(shortcutFolder, shortcutName);
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(settingsLink + ".lnk");
            shortcut.TargetPath = pathTo;
            shortcut.IconLocation = Path.GetFullPath(icon);
            shortcut.Arguments = "";
            shortcut.Description = "";
            shortcut.Save();
        }
        static string SanitizeFileName(string fileName)
        {
            char[] invalidFileNameCharacters = new char[]
            {
            '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\t', '\0'
            };

            foreach (char invalidChar in invalidFileNameCharacters)
            {
                fileName = fileName.Replace(invalidChar.ToString(), "");
            }

            return fileName;
        }
        static string getWebContent(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }
        static void ConvertImageToIcon(string imagePath, string iconPath)
        {
            using (MagickImage image = new MagickImage(imagePath))
            {
                image.Resize(256, 256); // Adjust the dimensions as needed for the icon size (e.g., 256x256)
                image.Format = MagickFormat.Icon;
                image.Write(iconPath);
            }
        }
    }
}