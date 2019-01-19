using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Ross.Services.Audio;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Serilog;

namespace Ross.Modules.Audio
{
    [Group("audio")]
    [RequireUserPermission(GuildPermission.MoveMembers, Group = "p", ErrorMessage = "This command requires Move Member permission!")]
    [RequireContext(ContextType.Guild)]
    [RequireOwner(Group = "p")]
    public class AudioModule : ModuleBase
    {
        private readonly AudioService audioService;

        AudioModule(AudioService audioService)
        {
            this.audioService = audioService;
        }

        [Command("stop", RunMode = RunMode.Async)]
        public async Task Leave()
        {
            await audioService.LeaveVoiceChannel(Context.Guild);
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task Play([Remainder] string path)
        {
            if (!audioService.IsGuildConnected(Context.Guild) ) await audioService.JoinVoiceChannel(Context.Guild, (Context.User as IVoiceState).VoiceChannel).ConfigureAwait(false);
            if (!File.Exists(path))
            {
                await this.ReplyAsync($"The file ({path}) you're trying to play does not exist!");
            }
            if (audioService.IsPlayingInGuild(Context.Guild))
            {
                await this.ReplyAsync("There's already music being played in this server!");
                return;
            }
            var client = (DiscordSocketClient)this.Context.Client;
            await client.SetActivityAsync(new Game(Path.GetFileName(path), ActivityType.Listening));
            await audioService.PlayLocalAudio(Context.Guild, path);
        }

    }
}
