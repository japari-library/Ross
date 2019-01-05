 
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
