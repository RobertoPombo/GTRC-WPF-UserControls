using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Net;

using GTRC_Basics;
using GTRC_Basics.Models;
using GTRC_Basics.Models.DTOs;
using GTRC_Database_Client.Responses;
using GTRC_Database_Client;
using GTRC_WPF_UserControls.ViewModels;

namespace GTRC_WPF_UserControls.Scripts
{
    public class DiscordCommands : ModuleBase<SocketCommandContext>
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
        public SocketUserMessage? UserMessage;
        public ulong ChannelId = GlobalValues.NoDiscordId;
        public ulong AuthorDiscordId = GlobalValues.NoDiscordId;
        public User? User;
        public Series? Series;
        public Season? Season;
        public DiscordChannelType? DiscordChannelType;
        public List<Role> AuthorRoles = [];
        public bool UserIsAdmin = false;
        public Role AdminRole = new();
        public Entry? Entry;

        public string AdminRoleTag { get { return "<@&" + AdminRole.DiscordId.ToString() + ">"; } }

        public DiscordCommands()
        {
            InitializeBase();
            DiscordBotConfigVM.ChangedDiscordBotIsActive += InitializeBase;
            DiscordBot.CommandNotFound += ExplainCommandsTrigger;
        }

        public void InitializeBase()
        {
            Initialize();
            _ = GetAdminRole();
        }

        public virtual void Initialize() { }

        public virtual async Task ExplainCommands() { }

        public void ExplainCommandsTrigger() { _ = ExplainCommands(); }

        public async Task GetAdminRole()
        {
            UniqPropsDto<Role> roleUniqDto = new() { Dto = new RoleUniqPropsDto0() { Name = AdminRoleName } };
            DbApiObjectResponse<Role> roleResponse = await DbApi.Connection.Role.GetByUniqProps(roleUniqDto);
            if (roleResponse.Status == HttpStatusCode.OK) { AdminRole = roleResponse.Object; }
        }

        public async Task SetDefaultProperties(SocketUserMessage? userMessage = null)
        {
            IsError = false;
            UserMessage = userMessage;
            UserMessage ??= Context?.Message ?? null;
            ChannelId = GlobalValues.NoDiscordId;
            AuthorDiscordId = GlobalValues.NoDiscordId;
            User = null;
            Series = null;
            Season = null;
            DiscordChannelType = null;
            AuthorRoles = [];
            UserIsAdmin = false;
            Entry = null;
            if (UserMessage is not null)
            {
                ChannelId = UserMessage.Channel.Id;
                AuthorDiscordId = UserMessage.Author.Id;
                UniqPropsDto<SeriesDiscordchanneltype> uniqDtoSerDis = new() { Index = 1, Dto = new SeriesDiscordchanneltypeUniqPropsDto1() { DiscordId = ChannelId } };
                DbApiObjectResponse<SeriesDiscordchanneltype> respObjSerDis = await DbApi.DynConnection.SeriesDiscordchanneltype.GetByUniqProps(uniqDtoSerDis);
                if (respObjSerDis.Status == HttpStatusCode.OK)
                {
                    DiscordChannelType = respObjSerDis.Object.DiscordChannelType;
                    Series = respObjSerDis.Object.Series;
                    DbApiObjectResponse<Season> respObjSea = await DbApi.DynConnection.Season.GetCurrent(Series.Id, DateTime.UtcNow);
                    if (respObjSea.Status == HttpStatusCode.OK) { Season = respObjSea.Object; }
                }
                UniqPropsDto<User> uniqDtoUser = new() { Index = 1, Dto = new UserUniqPropsDto1 { DiscordId = AuthorDiscordId } };
                DbApiObjectResponse<User> respObjUser = await DbApi.DynConnection.User.GetByUniqProps(uniqDtoUser);
                if (respObjUser.Status == HttpStatusCode.OK)
                {
                    User = respObjUser.Object;
                    DbApiListResponse<Role> respObjRole = await DbApi.Connection.Role.GetByUser(respObjUser.Object.Id);
                    if (respObjRole.Status == HttpStatusCode.OK) { AuthorRoles = respObjRole.List; }
                    foreach (Role role in AuthorRoles) { if (role.Name == AdminRoleName) { UserIsAdmin = true; break; } }
                }
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

        public async Task<bool> UserIsInDatabase(bool replyWithError = true)
        {
            if (User is null)
            {
                if (replyWithError)
                {
                    LogText = "Du bist nicht in der Datenbank eingetragen. Vermutlich sind die " + AdminRoleTag +
                        " noch nicht dazu gekommen, deine Discord-ID in der Datenbank zu hinterlegen.";
                    await ErrorResponse();
                }
                return false;
            }
            return true;
        }

        public async Task<bool> IsValidRaceNumber(string raceNumberStr, bool replyWithError = true)
        {
            if (ushort.TryParse(raceNumberStr, out ushort raceNumber) && raceNumber <= Entry.MaxRaceNumber && raceNumber >= Entry.MinRaceNumber) { return true; }
            if (replyWithError)
            {
                LogText = "Bitte eine gültige Startnummer angeben.";
                await ErrorResponse();
            }
            return false;
        }

        public async Task<bool> IsRegisteredRaceNumer(string raceNumberStr, bool replyWithError = true)
        {
            return await IsRegisteredRaceNumer(ParseRaceNumber(raceNumberStr), replyWithError);
        }

        public async Task<bool> IsRegisteredRaceNumer(ushort raceNumber, bool replyWithError = true)
        {
            if (Season is not null)
            {
                UniqPropsDto<Entry> uniqDtoEnt = new() { Dto = new EntryUniqPropsDto0() { SeasonId = Season.Id, RaceNumber = raceNumber, } };
                DbApiObjectResponse<Entry> respEnt = await DbApi.DynConnection.Entry.GetByUniqProps(uniqDtoEnt);
                if (respEnt.Status == HttpStatusCode.OK) { Entry = respEnt.Object; return true; }
            }
            if (replyWithError)
            {
                LogText = "Es ist kein Teilnehmer mit der Startnummer #" + raceNumber.ToString() + " für die Meisterschaft registriert.";
                await ErrorResponse();
            }
            Entry = null;
            return false;
        }

        public ushort ParseRaceNumber(string raceNumberStr)
        {
            if (ushort.TryParse(raceNumberStr, out ushort raceNumber)) { return raceNumber; }
            return ushort.MaxValue;
        }
    }
}
