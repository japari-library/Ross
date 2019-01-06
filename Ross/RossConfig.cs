 
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace Ross {
    public class RossConfigRoot
    {
        [JsonProperty("ross")]
        public Ross Ross { get; set; }

        [JsonProperty("playlist")]
        public Playlist Playlist { get; set; }

        [JsonProperty("binding")]
        public Binding Binding { get; set; }
    }

    public class Binding
    {
        [JsonProperty("serverID")]
        public ulong BindedServer { get; set; }

        [JsonProperty("channelID")]
        public ulong BindedChannel { get; set; }

        [JsonProperty("usePlaylist")]
        public string PlaylistName { get; set; }

        [JsonProperty("loop")]
        public bool Loop { get; set; }
    }

    public class Playlist
    {
        [JsonProperty("playlistDirectory")]
        public string PlaylistDirectory { get; set; }
    }

    public class Ross
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }

    public static class RossConfig
    {
        public static RossConfigRoot Root { get; private set; } = JsonConvert.DeserializeObject<RossConfigRoot>(File.ReadAllText("config.json"));
    }
}
