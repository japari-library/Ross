using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Ross.Services.Audio;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ross.Modules.Audio
{
    [Group("audio playlist")]
    [RequireUserPermission(GuildPermission.MoveMembers, Group = "p", ErrorMessage = "This command requires Move Member permission!")]
    [RequireContext(ContextType.Guild)]
    [RequireOwner(Group = "p")]
    public class PlaylistModule : ModuleBase
    {
        private readonly AudioService audioService;

        private readonly string PlaylistDirectory = RossConfig.Root.Playlist.PlaylistDirectory;

        private PlaylistModule(AudioService audioService)
        {
            this.audioService = audioService;
        }

        [Command("list")]
        public async Task ListPlaylist()
        {
            List<DirectoryInfo> subdirectoriesInfo = new DirectoryInfo(PlaylistDirectory).GetDirectories().ToList();
            List<string> subdirectoriesNames = new List<string>();
            subdirectoriesInfo.ForEach(d => subdirectoriesNames.Add(d.Name));
            await this.ReplyAsync($"Available playlist: ```{string.Join(",", subdirectoriesNames)}```");
        }

        [Command("start", RunMode = RunMode.Async)]
        public async Task StartPlaylist(string playlistName)
        {
            List<DirectoryInfo> playlistDirectory = new DirectoryInfo(PlaylistDirectory).GetDirectories().ToList();
            if (!playlistDirectory.Any(p => p.Name == playlistName))
            {
                await ReplyAsync($"{playlistName} does not exist in the playlist folder!");
                return;
            };

            if (!audioService.IsGuildConnected(Context.Guild))
                await audioService.JoinVoiceChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel).ConfigureAwait(false);

            var playlistService = audioService.GetPlaylist(Context.Guild);
            playlistService.AddFromPlaylistDirectory(playlistName);

            var client = (DiscordSocketClient)this.Context.Client;
            await playlistService.StartPlaylist(audioService, Context.Guild, client);
        }
    }
}
