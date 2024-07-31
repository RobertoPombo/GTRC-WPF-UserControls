using Discord.Commands;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

using GTRC_Basics.Configs;
using GTRC_WPF_UserControls.ViewModels;
using GTRC_Basics;

namespace GTRC_WPF_UserControls.Scripts
{
    public class DiscordBot
    {
        private CommandService commands = new();
        private InteractionService? interactions;
        private IServiceProvider? services;
        private DiscordSocketConfig socketConfig = new() { AlwaysDownloadUsers = false, GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent };

        public DiscordSocketClient? Client;
        public DiscordBotConfig? Config;

        public DiscordBot()
        {
            Initialize();
            DiscordBotConfigVM.ChangedDiscordBotIsActive += Initialize;
        }

        private Task ClientLog(LogMessage arg) { return Task.CompletedTask; }

        public void EnsureIsRunning()
        {
            if (Client is not null) { GlobalValues.CurrentLogText = "Discord bot is running."; }
            else { GlobalValues.CurrentLogText = "Discord bot stopped."; }
        }

        private async Task RegisterCommandsAsync()
        {
            if (Client is not null)
            {
                Client.MessageReceived += HandleCommandAsync;
                await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            }
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            if (Config is not null && Client is not null)
            {
                SocketUserMessage? userMessage = arg as SocketUserMessage;
                SocketCommandContext context = new(Client, userMessage);
                int argPos = 0;
                List<string> Tags = [];
                List<string> TagList = GetTagsByDiscordId(Config.DiscordBotId);
                foreach (string _tag in TagList) { Tags.Add(_tag + " "); }
                Tags.Add("!");
                foreach (string tag in Tags)
                {
                    if (userMessage?.HasStringPrefix(tag, ref argPos) ?? false)
                    {
                        var result = await commands.ExecuteAsync(context, argPos, services);
                        if (!result.IsSuccess && userMessage is not null)
                        {
                            await userMessage.DeleteAsync();
                        }
                    }
                }
            }
        }

        public async void Initialize()
        {
            Client?.Dispose();
            Config = DiscordBotConfig.GetActiveBot();
            if (Config is not null)
            {
                Client = new(socketConfig);
                services = new ServiceCollection().AddSingleton(Client).AddSingleton(commands).BuildServiceProvider();
                Client.Log += ClientLog;
                await RegisterCommandsAsync();
                await Client.LoginAsync(TokenType.Bot, Config.Token);
                await Client.StartAsync();
            }
        }

        public async Task SendMessage(string messageContent, ulong channelId)
        {
            if (Config is not null && Client is not null)
            {
                SocketTextChannel? channel = Client.GetGuild(Config.DiscordServerId)?.GetTextChannel(channelId);
                if (channel is not null) { await SendMessageRecursive(channel, messageContent); }
            }
        }

        public async Task SendMessageRecursive(SocketTextChannel channel, string messageContent)
        {
            List<string> keys = ["**\n", "\n"];
            string part1;
            string part2;
            if (Config is not null && messageContent.Length > Config.CharLimit)
            {
                int keyNr = 0;
                string key = keys[keyNr];
                int charPos = Config.CharLimit;
                while (true)
                {
                    charPos--;
                    if (charPos == key.Length && keyNr < keys.Count - 1)
                    {
                        keyNr++;
                        key = keys[keyNr];
                        charPos = Config.CharLimit;
                    }
                    else if (charPos == key.Length)
                    {
                        part1 = messageContent[..Config.CharLimit];
                        part2 = messageContent[Config.CharLimit..];
                        break;
                    }
                    else if (messageContent.Substring(charPos - key.Length, key.Length) == key)
                    {
                        part1 = messageContent[..charPos];
                        part2 = messageContent[charPos..];
                        break;
                    }
                }
                await SendMessage(channel, part1);
                await SendMessageRecursive(channel, part2);
            }
            else
            {
                await SendMessage(channel, messageContent);
            }
        }

        public static async Task SendMessage(SocketTextChannel channel, string MessageContent)
        {
            await channel.SendMessageAsync(MessageContent);
        }

        public static List<string> GetTagsByDiscordId(ulong discordId)
        {
            List<string> TagList = ["<@" + discordId.ToString() + ">", "<@!" + discordId.ToString() + ">"];
            return TagList;
        }
    }
}
