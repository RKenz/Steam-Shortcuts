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
        static string programs_path = "";
        static string shortcutFolder = "";
        static void Main(string[] args)
        {
            SteamResponse respon = GetGameData();

            programs_path = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            shortcutFolder = Path.Combine(programs_path, @"SteamAppShortcuts");
            Directory.Delete(shortcutFolder, true);
            Directory.CreateDirectory(shortcutFolder);
            string iconFolder = Path.Combine(shortcutFolder, $"icon");
            Directory.CreateDirectory(iconFolder);

            Console.WriteLine($"==========\nApp Name\n==========");
            Parallel.ForEach(respon.apps, (game) =>
            {
                string gameName = SanitizeFileName(game.name);
                Console.WriteLine(gameName);
                string icon = Path.Combine(iconFolder , $"{game.appid}.ico");
                DownloadImage(game, icon);
                AddShortcut($"steam://rungameid/{game.appid}", gameName, icon);
            });
            Console.WriteLine("\nFinish...\nPress any key to close the app.");
            Console.Read();
        }

        private static void DownloadImage(SteamApp game, string destination)
        {
            string imgHash = game.icon;
            string imageURL = $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{game.appid}/{imgHash}.jpg";
            string image = $"{game.appid}.jpg";

            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] dataArr = client.DownloadData(imageURL);
                    System.IO.File.WriteAllBytes(image, dataArr);
                    ConvertImageToIcon(image, destination);
                }
            }
            catch
            {
                Console.WriteLine("Error downloading icon");
            }
            finally
            {
                if(System.IO.File.Exists(image))
                {
                    System.IO.File.Delete(image);
                }
            }
        }

        private static SteamResponse GetGameData()
        {
            string apiURL = $"https://api.steampowered.com/ICommunityService/GetApps/v1/?";
            int i = 0;
            foreach (string id in getAppIDs())
            {
                apiURL += $"&appids%5B{i++}%5D={id}";
            }
            string gameContent = getWebContent(apiURL);
            return JsonConvert.DeserializeObject<Root>(gameContent).response;
        }

        private static List<string> getAppIDs()
        {
            try
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
                if (key != null)
                {
                    string vdfLoc = Path.Combine(key.GetValue("InstallPath").ToString(), "steamapps", "libraryfolders.vdf");
                    key.Close();
                    if(!System.IO.File.Exists(vdfLoc)) throw new Exception("Error locating steam library");

                    List<string> list = listAppIDs(vdfLoc);
                    return list;
                }
                throw new Exception("Error locating steam library");
            }
            catch
            {
                RetryVDF:
                Console.WriteLine("Error locating steam library.\nInput libraryfolders.vdf file location (ex. C:\\Program Files (x86)\\Steam\\steamapps\\libraryfolders.vdf).");
                string vdfLoc = Console.ReadLine();
                if (!System.IO.File.Exists(vdfLoc)) goto RetryVDF;
                List<string> list = listAppIDs(vdfLoc);
                return list;
            }
            return null;
        }

        private static List<string> listAppIDs(string vdfLoc)
        {
            List<string> list = new List<string>();
            string vdf = System.IO.File.ReadAllText(vdfLoc);
            Regex regex = new Regex("\"apps\"\n\t\t{[^}]*\\}", RegexOptions.IgnoreCase);
            MatchCollection apps = regex.Matches(vdf);
            Console.WriteLine($"==========\nFound App ID\n==========");
            foreach (Match app in apps.ToArray())
            {
                string values = app.Value.Replace("\t", "").Replace("\"apps\"\n{\n", "");
                if (values.Length - 2 <= 0) continue;
                values = values.Substring(0, values.Length - 2);

                foreach (string value in values.Split('\n'))
                {
                    string appID = value.Substring(1, value.Substring(1).IndexOf('\"'));
                    Console.WriteLine(appID);
                    list.Add(appID);
                }
            }
            return list;
        }

        private static void AddShortcut(string pathTo, string shortcutName, string icon)
        {

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
                image.Resize(256, 256);
                image.Format = MagickFormat.Icon;
                image.Write(iconPath);
            }
        }
    }
}