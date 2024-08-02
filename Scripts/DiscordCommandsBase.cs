using Discord;
using Discord.Commands;
using System.Net;

using GTRC_Basics;
using GTRC_Basics.Models;
using GTRC_Basics.Models.DTOs;
using GTRC_Database_Client.Responses;
using GTRC_Database_Client;
using GTRC_WPF_UserControls.ViewModels;

namespace GTRC_WPF_UserControls.Scripts
{
    public class DiscordCommandsBase : ModuleBase<SocketCommandContext>
    {
        public static readonly DiscordBot DiscordBot = new();

        public static readonly Emoji EmojiSuccess = new("✅");
        public static readonly Emoji EmojiFail = new("❌");
        public static readonly Emoji EmojiWTF = new("🤷‍♀️");
        public static readonly Emoji EmojiSleep = new("😴");
        public static readonly Emoji EmojiShocked = new("😱");
        public static readonly Emoji EmojiRaceCar = new("🏎️");
        public static readonly Emoji EmojiPartyFace = new("🥳");
        public static readonly Emoji EmojiCry = new("😭");
        public static readonly Emoji EmojiThinking = new("🤔");

        private static readonly string AdminRoleName = "Communityleitung";

        public bool IsError = false;
        public string LogText = string.Empty;
        public ulong ChannelId = GlobalValues.NoDiscordId;
        public ulong AuthorDiscordId = GlobalValues.NoDiscordId;
        public User? User;
        public List<Role> AuthorRoles = [];
        public bool UserIsAdmin = false;
        public Season? Season;
        public Role AdminRole = new();

        public string AdminRoleTag { get { return "<@&" + AdminRole.DiscordId.ToString() + ">"; } }

        public DiscordCommandsBase()
        {
            InitializeBase();
            DiscordBotConfigVM.ChangedDiscordBotIsActive += InitializeBase;
        }

        public void InitializeBase()
        {
            Initialize();
            _ = GetAdminRole();
        }

        public virtual void Initialize() { }

        public async Task GetAdminRole()
        {
            UniqPropsDto<Role> roleUniqDto = new() { Dto = new RoleUniqPropsDto0() { Name = AdminRoleName } };
            DbApiObjectResponse<Role> roleResponse = await DbApi.Connection.Role.GetByUniqProps(roleUniqDto);
            if (roleResponse.Status == HttpStatusCode.OK) { AdminRole = roleResponse.Object; }
        }

        public async Task SetDefaultProperties()
        {
            IsError = false;
            if (DiscordBot.Config is not null)
            {
                DbApiObjectResponse<Season> seasonResponse = await DbApi.Connection.Season.GetById(DiscordBot.Config.SeasonId);
                if (seasonResponse.Status == HttpStatusCode.OK) { Season = seasonResponse.Object; }
            }
            if (Context is not null) { AuthorDiscordId = Context.Message.Author.Id; ChannelId = Context.Message.Channel.Id; }
            else { AuthorDiscordId = GlobalValues.NoDiscordId; ChannelId = GlobalValues.NoDiscordId; }
            UniqPropsDto<User> uniqUserDto = new() { Index = 1, Dto = new UserUniqPropsDto1 { DiscordId = AuthorDiscordId } };
            DbApiObjectResponse<User> userResponse = await DbApi.DynConnection.User.GetByUniqProps(uniqUserDto);
            if (userResponse.Status == HttpStatusCode.OK)
            {
                User = userResponse.Object;
                DbApiListResponse<Role> roleResponse = await DbApi.Connection.Role.GetByUser(userResponse.Object.Id);
                if (roleResponse.Status == HttpStatusCode.OK) { AuthorRoles = roleResponse.List; }
                foreach (Role role in AuthorRoles) { if (role.Name == AdminRoleName) { UserIsAdmin = true; break; } }
            }
        }

        public async Task ErrorResponse()
        {
            if (!IsError)
            {
                if (LogText.Length > 0) { await ReplyAsync(LogText); }
                if (Context is not null)
                {
                    await Context.Message.RemoveAllReactionsAsync();
                    await Context.Message.AddReactionAsync(EmojiFail);
                }
                LogText = string.Empty;
                IsError = true;
            }
        }
    }
}
