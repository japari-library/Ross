using Discord;
using Discord.Audio;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ross.Services.Audio
{
    public class AudioService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedServers = new ConcurrentDictionary<ulong, IAudioClient>();

        private readonly Dictionary<ulong, bool> PlayingStates = new Dictionary<ulong, bool>();

        private readonly ConcurrentDictionary<ulong, AudioOutStream> StreamsCache = new ConcurrentDictionary<ulong, AudioOutStream>();

        private readonly ConcurrentDictionary<ulong, PlaylistService> PlaylistInstances = new ConcurrentDictionary<ulong, PlaylistService>();

        private readonly Dictionary<ulong, CancellationTokenSource> CancellationTokenSources = new Dictionary<ulong, CancellationTokenSource>();

        public async Task JoinVoiceChannel(IGuild guild, IVoiceChannel targetChannel)
        {
            if (IsGuildConnected(guild))
            {
                return;
            }

            IAudioClient audioClient = await targetChannel.ConnectAsync();
            if (ConnectedServers.TryAdd(guild.Id, audioClient) && PlaylistInstances.TryAdd(guild.Id, new PlaylistService()))
            {
                PlayingStates.Add(guild.Id, false);
                Log.Information($"Successfully joined a voice channel in {guild.Id}");
            }
        }

        public PlaylistService GetPlaylist(IGuild guild)
        {
            return PlaylistInstances[guild.Id];
        }

        public async Task LeaveVoiceChannel(IGuild guild)
        {
            if (ConnectedServers.ContainsKey(guild.Id))
            {
                Log.Information($"Left a voice channel in {guild.Id}");
                await ConnectedServers[guild.Id].StopAsync();
                PlayingStates[guild.Id] = false;
                PlaylistInstances[guild.Id].Stop();

                // we don't want these result, so let's just store it and forget about it
                if (StreamsCache.ContainsKey(guild.Id)) StreamsCache.TryRemove(guild.Id, out var outStream);
                ConnectedServers.TryRemove(guild.Id, out var client);
            }
        }

        public bool IsGuildConnected(IGuild guild)
        {
            return ConnectedServers.ContainsKey(guild.Id);
        }

        public bool IsPlayingInGuild(IGuild guild)
        {
            if (PlayingStates.ContainsKey(guild.Id))
            {
                return PlayingStates.Where(id => id.Key == guild.Id).First().Value;
            }
            else
            {
                return false;
            }
        }

        public async Task PlayLocalAudio(IGuild guild, string path)
        {
            if (IsPlayingInGuild(guild))
            {
                Log.Error($"[AudioService] {guild.Id} is already playing audio!");
                return;
            }

            if (!File.Exists(path))
            {
                Log.Error($"[AudioService] [{guild.Id}] {path} does not exist");
                return;
            };

            PlayingStates[guild.Id] = true;
            IAudioClient client = ConnectedServers[guild.Id];
            Log.Information($"[AudioService] Playing {path} at {guild.Id}");
            using (Encoder encoder = new Encoder())
            {
                using (var outputStream = await encoder.Encode(path))
                {
                    AudioOutStream discordStream;

                    // we want to stay in the same channel, so let's reuse the stream
                    if (!StreamsCache.ContainsKey(guild.Id))
                    {
                        discordStream = client.CreatePCMStream(AudioApplication.Music);
                        StreamsCache.TryAdd(guild.Id, discordStream);
                    }
                    else
                    {
                        discordStream = StreamsCache[guild.Id];
                    }
                    
                    await outputStream.CopyToAsync(discordStream);
                    await discordStream.FlushAsync().ConfigureAwait(false);
                }
            }
            Log.Information($"[AudioService] Stopped playing {path}");
            PlayingStates[guild.Id] = false;
        }

    }
}
