namespace Create_Steam_Shortcuts
{
    internal class steam_API_JSON
    {
        public class steamToken
        {
            public string token;
        }
        public class SteamApp
        {
            public int appid { get; set; }
            public string name { get; set; }
            public string icon { get; set; }
            public bool community_visible_stats { get; set; }
            public string propagation { get; set; }
            public bool has_adult_content { get; set; }
            public int app_type { get; set; }
            public bool has_adult_content_violence { get; set; }
            public List<int> content_descriptorids { get; set; }
            public bool? is_visible_in_steam_china { get; set; }
            public bool? tool { get; set; }
        }

        public class SteamResponse
        {
            public List<SteamApp> apps { get; set; }
        }

        public class Root
        {
            public SteamResponse response { get; set; }
        }
    }
}
