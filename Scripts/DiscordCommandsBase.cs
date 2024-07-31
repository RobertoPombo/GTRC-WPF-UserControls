using Discord;
using Discord.Commands;
using System.Net;

using GTRC_Basics;
using GTRC_Basics.Configs;
using GTRC_Basics.Models;
using GTRC_Basics.Models.DTOs;
using GTRC_Database_Client;
using GTRC_Database_Client.Responses;
using GTRC_WPF_UserControls.ViewModels;

namespace GTRC_WPF_UserControls.Scripts
{
    public class DiscordCommandsBase : ModuleBase<SocketCommandContext>
    {
        public static DiscordCommandsBase? Instance;
        public static readonly DiscordBot DiscordBot = new();

        public static readonly Emoji emojiSuccess = new("✅");
        public static readonly Emoji emojiFail = new("❌");
        public static readonly Emoji emojiWTF = new("🤷‍♀️");
        public static readonly Emoji emojiSleep = new("😴");
        public static readonly Emoji emojiShocked = new("😱");
        public static readonly Emoji emojiRaceCar = new("🏎️");
        public static readonly Emoji emojiPartyFace = new("🥳");
        public static readonly Emoji emojiCry = new("😭");
        public static readonly Emoji emojiThinking = new("🤔");

        public DiscordBotConfig? Config;
        public bool IsError = false;
        public string LogText = string.Empty;
        public ulong AuthorDiscordId = GlobalValues.NoDiscordId;
        public User? User;
        public List<Role> AuthorRoles = [];
        public bool UserIsAdmin = false;
        public Season? Season;

        private static readonly string AdminRoleName = "Communityleitung";

        public DiscordCommandsBase()
        {
            Instance = this;
            Initialize();
            DiscordBotConfigVM.ChangedDiscordBotIsActive += Initialize;
        }

        public void Initialize()
        {
            Config = DiscordBotConfig.GetActiveBot();
        }

        public async Task SetDefaultProperties()
        {
            IsError = false;
            AuthorDiscordId = Context.Message.Author.Id;
            UniqPropsDto<User> uniqUserDto = new() { Index = 1, Dto = new UserUniqPropsDto1 { DiscordId = AuthorDiscordId } };
            DbApiObjectResponse<User> userResponse = await DbApi.DynConnection.User.GetByUniqProps(uniqUserDto);
            if (userResponse.Status == HttpStatusCode.OK)
            {
                User = userResponse.Object;
                DbApiListResponse<Role> roleResponse = await DbApi.Connection.Role.GetByUser(userResponse.Object.Id);
                if (roleResponse.Status == HttpStatusCode.OK) { AuthorRoles = roleResponse.List; }
                foreach (Role role in AuthorRoles) { if (role.Name == AdminRoleName) { UserIsAdmin = true; break; } }
            }
            if (Config is not null)
            {
                DbApiObjectResponse<Season> seasonResponse = await DbApi.Connection.Season.GetById(Config.SeasonId);
                if (seasonResponse.Status == HttpStatusCode.OK) { Season = seasonResponse.Object; }
            }
        }

        public async Task ErrorResponse()
        {
            if (!IsError)
            {
                if (LogText.Length > 0) { await ReplyAsync(LogText); }
                await Context.Message.RemoveAllReactionsAsync();
                await Context.Message.AddReactionAsync(emojiFail);
                LogText = string.Empty;
                IsError = true;
            }
        }

        public (List<dynamic>, int, ComponentBuilder) AddOption2Menu(string value, string name, List<dynamic> ListMenu, int optionCount, ComponentBuilder component, string id, string placeholder)
        {
            int countListMenu = ListMenu.Count;
            optionCount++;
            if (optionCount > 25)
            {
                ListMenu.Add(new SelectMenuBuilder() { CustomId = id + countListMenu.ToString(), Placeholder = placeholder });
                optionCount = 1; countListMenu++;
                component.WithSelectMenu(ListMenu[countListMenu - 2]);
            }
            ListMenu[countListMenu - 1].AddOption(value, name);
            return (ListMenu, optionCount, component);
        }
    }
}
