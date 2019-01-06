using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Ross;
using Ross.Services.Audio;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

public class Program
{
    private CommandService commands;
    private DiscordSocketClient client;
    private IServiceProvider services;
    private AudioService audioService;

    private static void Main(string[] args)
    {
        new Program().Start().GetAwaiter().GetResult();
    }

    public async Task Start()
    {
        client = new DiscordSocketClient();
        commands = new CommandService();
        audioService = new AudioService();

        Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();


        services = new ServiceCollection()
                .AddSingleton(audioService)
                .BuildServiceProvider();

        await InstallCommands();

        await client.LoginAsync(TokenType.Bot, RossConfig.Root.Ross.Token);
        await client.StartAsync();

        client.Connected += async () => Log.Information($"{client.CurrentUser.Username} has connected to Discord!");
        client.GuildAvailable += (guild) => Task.Run(() => OnGuildReady(guild).ConfigureAwait(false));
        await Task.Delay(-1);
    }

    public async Task InstallCommands()
    {
        // Hook the MessageReceived Event into our Command Handler
        client.MessageReceived += HandleCommand;
        // Discover all of the commands in this assembly and load them.
        await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
    }

    public async Task OnGuildReady(SocketGuild guild)
    {
        if (guild.CurrentUser.VoiceChannel != null && guild.VoiceChannels.Count > 1)
        {
            // seems like the bot died during VC, this'll cause issue such as the bot being slient and stuffs so let's force it out
            Log.Information("Seems like the bot died while in a voice chat, forcing disconnect...");
            await audioService.JoinVoiceChannel(guild, guild.CurrentUser.VoiceChannel);
            await audioService.LeaveVoiceChannel(guild);
            Log.Information($"Successfully disconnected the bot from the VC in {guild.Id}!");
        }

        if (RossConfig.Root.Binding.BindedChannel > 0 && RossConfig.Root.Binding.BindedServer == guild.Id)
        {

            Binding bindingConfig = RossConfig.Root.Binding;
            Log.Information($"Joining {bindingConfig.BindedChannel} in {guild.Id} due to binding.");
            SocketVoiceChannel voiceChannel = guild.GetVoiceChannel(RossConfig.Root.Binding.BindedChannel);
            await audioService.JoinVoiceChannel(guild, voiceChannel);
            var playlist = audioService.GetPlaylist(guild);
            playlist.AddFromPlaylistDirectory(RossConfig.Root.Binding.PlaylistName);
            playlist.Loop = bindingConfig.Loop;
            await Task.Run(() => playlist.StartPlaylist(audioService, guild, this.client)).ConfigureAwait(false);
        }
    }

    public async Task HandleCommand(SocketMessage messageParam)
    {
        // Don't process the command if it was a System Message
        SocketUserMessage message = messageParam as SocketUserMessage;
        if (message == null)
        {
            return;
        }
        // Create a number to track where the prefix ends and the command begins
        int argPos = 0;
        // Determine if the message is a command, based on if it starts with '+' or a mention prefix
        if (!(message.HasCharPrefix('+', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos)))
        {
            return;
        }
        // Create a Command Context
        CommandContext context = new CommandContext(client, message);
        // Execute the command. (result does not indicate a return value, 
        // rather an object stating if the command executed successfully)
        IResult result = await commands.ExecuteAsync(context, argPos, services);
        if (!result.IsSuccess)
        {
            if (result.Error != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}