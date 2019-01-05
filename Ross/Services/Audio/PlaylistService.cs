using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Serilog;
using Discord.WebSocket;

namespace Ross.Services.Audio
{
    public class PlaylistService
    {
        private List<string> PathList = new List<string>();

        public bool Loop { get; private set; }

        public bool IsPlaying { get; private set; }

        public void AddPath(string path)
        {
            if (File.Exists(path)) PathList.Add(path);
        }

        public void RemovePath(string path)
        {
            PathList.Remove(PathList.Where(p => p == path).First());
        }

        public void ToggleLoop()
        {
            Loop = !Loop;
        }

        public void Stop()
        {
            IsPlaying = false;
            PathList.Clear();
        }

        public void Randomize()
        {
            var rng = new Random();
            PathList.OrderBy(p => rng.Next());
        }

        public async Task StartPlaylist(AudioService audioService, IGuild guild, DiscordSocketClient client)
        {
            IsPlaying = true;
            while (PathList.Count > 0)
            {
                if (!IsPlaying) return;
                Log.Information($"[PlaylistService] Playing {PathList.First()}");
                await client.SetActivityAsync(new Game(Path.GetFileNameWithoutExtension(PathList.First()), ActivityType.Listening));
                await audioService.PlayLocalAudio(guild, PathList.First());
                if (Loop == true)
                {
                    // then we add it to the bottom of the queue, and then delete the top
                    var path = PathList.First();
                    PathList.Add(path);
                }
                PathList.Remove(PathList.First());
                Log.Information($"[PlaylistService] Removed the last played song off the queue, now {PathList.First()} is at the front");
                await Task.Delay(2000);
            }
            IsPlaying = false;
        }
    }
}
