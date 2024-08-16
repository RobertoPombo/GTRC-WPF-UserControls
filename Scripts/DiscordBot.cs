using Discord.Commands;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

using GTRC_Basics.Configs;
using GTRC_WPF_UserControls.ViewModels;
using GTRC_Basics;
using GTRC_WPF_UserControls.Models;
using GTRC_WPF;

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

        public SocketUserMessage? UserMessage;

        public DiscordBot()
        {
            Initialize();
            DiscordBotConfigVM.ChangedDiscordBotIsActive += Initialize;
            TemporaryDiscordMessage.LoadJson();
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
                UserMessage = arg as SocketUserMessage;
                SocketCommandContext context = new(Client, UserMessage);
                int argPos = 0;
                List<string> Tags = [];
                List<string> TagList = GetTagsByDiscordId(Config.DiscordBotId);
                foreach (string _tag in TagList) { Tags.Add(_tag + " "); }
                Tags.Add("!");
                foreach (string tag in Tags)
                {
                    if (UserMessage?.HasStringPrefix(tag, ref argPos) ?? false)
                    {
                        var result = await commands.ExecuteAsync(context, argPos, services);
                        if (!result.IsSuccess && UserMessage is not null) { OnCommandNotFound(); }
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

        public async Task SendMessage(string messageContent, ulong channelId, DiscordMessageType discordMessageType)
        {
            if (Config is not null && Client is not null)
            {
                SocketTextChannel? channel = Client.GetGuild(Config.DiscordServerId)?.GetTextChannel(channelId);
                if (channel is not null)
                {
                    TemporaryDiscordMessage newMessage = new() { ChannelId = channelId, Type = discordMessageType };
                    for (int index = TemporaryDiscordMessage.List.Count - 1; index >= 0; index--)
                    {
                        if (TemporaryDiscordMessage.List[index].ChannelId == newMessage.ChannelId && newMessage.DoesOverride(TemporaryDiscordMessage.List[index]))
                        {
                            await DeleteMessage(channel, TemporaryDiscordMessage.List[index].MessageId);
                            TemporaryDiscordMessage.List.RemoveAt(index);
                        }
                    }
                    TemporaryDiscordMessage.SaveJson();
                    await SendMessageRecursive(channel, messageContent, discordMessageType);
                }
            }
        }

        private async Task SendMessageRecursive(SocketTextChannel channel, string messageContent, DiscordMessageType discordMessageType)
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
                await SendMessage(channel, part1, discordMessageType);
                await SendMessageRecursive(channel, part2, discordMessageType);
            }
            else
            {
                await SendMessage(channel, messageContent, discordMessageType);
            }
        }

        private static async Task SendMessage(SocketTextChannel channel, string MessageContent, DiscordMessageType discordMessageType)
        {
            IUserMessage newUserMessage = await channel.SendMessageAsync(MessageContent);
            TemporaryDiscordMessage newMessage = new() { MessageId = newUserMessage.Id, ChannelId = channel.Id, Type = discordMessageType };
            TemporaryDiscordMessage.List.Add(newMessage);
            TemporaryDiscordMessage.SaveJson();
        }

        private static async Task DeleteMessage(SocketTextChannel channel, ulong messageId)
        {
            IMessage? oldMessage = null;
            if (channel is not null) { try { oldMessage = await channel.GetMessageAsync(messageId); } catch { } }
            if (oldMessage is not null) { await oldMessage.DeleteAsync(); }
        }

        public static List<string> GetTagsByDiscordId(ulong discordId)
        {
            List<string> TagList = ["<@" + discordId.ToString() + ">", "<@!" + discordId.ToString() + ">"];
            return TagList;
        }

        public static event Notify? CommandNotFound;

        public static void OnCommandNotFound() { CommandNotFound?.Invoke(); }
    }
}
