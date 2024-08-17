﻿using Discord;
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
        private static readonly TimeSpan DurationTimeoutExplainCommands = TimeSpan.FromSeconds(1);

        private static DateTime StartTimeoutExplainCommands = DateTime.UtcNow;

        public bool IsError = false;
        public string LogText = string.Empty;
        public SocketUserMessage? UserMessage;
        public ulong ChannelId = GlobalValues.NoDiscordId;
        public DateTime? Date = DateTime.UtcNow;
        public ulong AuthorDiscordId = GlobalValues.NoDiscordId;
        public User? User;
        public Series? Series;
        public Season? Season;
        public DiscordChannelType? DiscordChannelType;
        public List<Role> AuthorRoles = [];
        public bool UserIsAdmin = false;
        public Role AdminRole = new();
        public Event? Event;
        public Entry? Entry;
        public EntryEvent? EntryEvent;
        public EntryUserEvent? EntryUserEvent;
        public Car? Car;

        public string AdminRoleTag { get { return "<@&" + AdminRole.DiscordId.ToString() + ">"; } }
        public string LogUnknownError { get { return AdminRoleTag + " Unbekannter Fehler."; } }
        public string EntryToString { get { if (Entry is null) { return string.Empty; } return "#" + Entry.RaceNumber.ToString() + " " + Entry.Team.Name; } }

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

        public void ExplainCommandsTrigger()
        {
            if (DateTime.UtcNow - StartTimeoutExplainCommands > DurationTimeoutExplainCommands)
            {
                StartTimeoutExplainCommands = DateTime.UtcNow;
                _ = ExplainCommands();
            }
        }

        public virtual async Task ExplainCommands() { }

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
            Date = DateTime.UtcNow;
            AuthorDiscordId = GlobalValues.NoDiscordId;
            User = null;
            Series = null;
            Season = null;
            DiscordChannelType = null;
            AuthorRoles = [];
            UserIsAdmin = false;
            Event = null;
            Entry = null;
            EntryEvent = null;
            EntryUserEvent = null;
            Car = null;
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
                    if (respObjSea.Status == HttpStatusCode.OK)
                    {
                        Season = respObjSea.Object;
                        DbApiObjectResponse<Event> respObjEve = await DbApi.DynConnection.Event.GetNext(Season.Id);
                        if (respObjEve.Status == HttpStatusCode.OK) { Event = respObjEve.Object; }
                    }
                }
                UniqPropsDto<User> uniqDtoUser = new() { Index = 1, Dto = new UserUniqPropsDto1 { DiscordId = AuthorDiscordId } };
                DbApiObjectResponse<User> respObjUser = await DbApi.DynConnection.User.GetByUniqProps(uniqDtoUser);
                if (respObjUser.Status == HttpStatusCode.OK)
                {
                    User = respObjUser.Object;
                    DbApiListResponse<Role> respListRole = await DbApi.DynConnection.Role.GetByUser(User.Id);
                    AuthorRoles = (await DbApi.DynConnection.Role.GetByUser(User.Id)).List;
                    foreach (Role role in AuthorRoles) { if (role.Name == AdminRoleName) { UserIsAdmin = true; break; } }
                }
            }
        }

        public async Task ErrorResponse()
        {
            if (!IsError)
            {
                if (Context is not null)
                {
                    await Context.Message.RemoveAllReactionsAsync();
                    _ = Context.Message.AddReactionAsync(EmojiFail);
                }
                if (LogText.Length > 0) { _ = ReplyAsync(LogText); }
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
                    if (UserIsAdmin) { LogText = "Der Fahrer ist nicht in der Datenbank eingetragen. " + AdminRoleTag + " Möglicherweise fehlt seine Discord-ID."; }
                    else
                    {
                        LogText = "Du bist nicht in der Datenbank eingetragen. Vermutlich sind die " + AdminRoleTag +
                            " noch nicht dazu gekommen, deine Discord-ID in der Datenbank zu hinterlegen.";
                    }
                    await ErrorResponse();
                }
                return false;
            }
            return true;
        }

        public async Task<bool> UserIsOnEntry(bool replyWithError = true)
        {
            if (User is not null && Entry is not null)
            {
                DbApiListResponse<User> respListUser = await DbApi.DynConnection.User.GetByEntry(Entry.Id);
                foreach (User user in respListUser.List) { if (user.Id == User.Id) { return true; } }
                if (replyWithError && !UserIsAdmin)
                {
                    LogText = "Du bist nicht dazu berechtigt, Änderungen für den Teilnehmer #" + Entry.RaceNumber.ToString() + " " + Entry.Team.Name + " vorzunehmen.";
                    await ErrorResponse();
                }
            }
            return false;
        }

        public async Task<bool> IsValidRaceNumber(string strRaceNumber, bool replyWithError = true)
        {
            return await IsValidRaceNumber(ParseRaceNumber(strRaceNumber), replyWithError);
        }

        public async Task<bool> IsValidRaceNumber(ushort raceNumber, bool replyWithError = true)
        {
            if (raceNumber <= Entry.MaxRaceNumber && raceNumber >= Entry.MinRaceNumber) { return true; }
            if (replyWithError)
            {
                LogText = "Bitte eine gültige Startnummer angeben.";
                await ErrorResponse();
            }
            return false;
        }

        public async Task<bool> IsRegisteredRaceNumer(string strRaceNumber, bool replyWithError = true)
        {
            return await IsRegisteredRaceNumer(ParseRaceNumber(strRaceNumber), replyWithError);
        }

        public async Task<bool> IsRegisteredRaceNumer(ushort raceNumber, bool replyWithError = true)
        {
            Entry = null;
            EntryEvent = null;
            EntryUserEvent = null;
            if (Season is not null && await IsValidRaceNumber(raceNumber, replyWithError))
            {
                UniqPropsDto<Entry> uniqDtoEnt = new() { Dto = new EntryUniqPropsDto0() { SeasonId = Season.Id, RaceNumber = raceNumber, } };
                DbApiObjectResponse<Entry> respObjEnt = await DbApi.DynConnection.Entry.GetByUniqProps(uniqDtoEnt);
                if (respObjEnt.Status == HttpStatusCode.OK) { Entry = respObjEnt.Object; return true; }
                if (replyWithError)
                {
                    LogText = "Es ist kein Teilnehmer mit der Startnummer #" + raceNumber.ToString() + " für die Meisterschaft registriert.";
                    await ErrorResponse();
                }
            }
            return false;
        }

        public async Task<bool> IsValidEventNr(string strEventNr, bool replyWithError = true)
        {
            return await IsValidEventNr(ParseEventNr(strEventNr), replyWithError);
        }

        public async Task<bool> IsValidEventNr(int eventNr, bool replyWithError = true)
        {
            Event = null;
            EntryEvent = null;
            EntryUserEvent = null;
            if (Season is not null)
            {
                DbApiObjectResponse<Event> respObjEve = await DbApi.DynConnection.Event.GetByNr(Season.Id, eventNr);
                if (respObjEve.Status == HttpStatusCode.OK) { Event = respObjEve.Object; return true; }
                if (replyWithError)
                {
                    LogText = "Bitte eine gültige Event-Nr angeben.";
                    DbApiListResponse<Event> respListEve = await DbApi.DynConnection.Event.GetChildObjects(typeof(Season), Season.Id);
                    if (respListEve.Status == HttpStatusCode.OK)
                    {
                        int eventsCount = 0;
                        foreach (Event _event in respListEve.List) { if (!_event.IsPreQualifying) { eventsCount++; } }
                        LogText = "Bitte eine Event-Nr zwischen 1 und " + eventsCount.ToString() + " angeben.";
                    }
                    LogText += " Details zu den Events findest du im `!kalender`.";
                    await ErrorResponse();
                }
            }
            return false;
        }

        public async Task<bool> IsRegisteredUserForSeason(bool replyWithError = true)
        {
            Entry = null;
            EntryEvent = null;
            EntryUserEvent = null;
            if (Season is not null && User is not null)
            {
                DbApiListResponse<Entry> respListEnt = await DbApi.DynConnection.Entry.GetByUserSeason(User.Id, Season.Id);
                if (respListEnt.List.Count > 0) { Entry = respListEnt.List[0]; return true; }
                if (replyWithError)
                {
                    LogText = "Du bist noch nicht für die Meisterschaft registriert.";
                    await ErrorResponse();
                }
            }
            return false;
        }

        public async Task<bool> IsRegisteredUserForEvent(bool replyWithError = true)
        {
            Entry = null;
            EntryEvent = null;
            EntryUserEvent = null;
            if (User is not null && Event is not null)
            {
                DbApiListResponse<Entry> respListEnt = await DbApi.DynConnection.Entry.GetByUserEvent(User.Id, Event.Id);
                if (respListEnt.List.Count > 0) { Entry = respListEnt.List[0]; return true; }
                if (replyWithError)
                {
                    LogText = "Du bist noch nicht für dieses Event registriert.";
                    await ErrorResponse();
                }
            }
            return false;
        }

        public async Task<bool> IsValidAccCarId(string strAccCarId, bool replyWithError = true)
        {
            return await IsValidAccCarId(ParseAccCarId(strAccCarId), replyWithError);
        }

        public async Task<bool> IsValidAccCarId(uint accCarId, bool replyWithError = true)
        {
            Car = null;
            if (Season is not null)
            {
                UniqPropsDto<Car> uniqDtoCar = new() { Index = 1, Dto = new CarUniqPropsDto1() { AccCarId = accCarId } };
                DbApiObjectResponse<Car> respObjCar = await DbApi.DynConnection.Car.GetByUniqProps(uniqDtoCar);
                if (respObjCar.Status == HttpStatusCode.OK)
                {
                    DbApiListResponse<SeasonCarclass> respListSeaCarcl = await DbApi.DynConnection.SeasonCarclass.GetChildObjects(typeof(Season), Season.Id);
                    foreach (SeasonCarclass seasonCarclass in respListSeaCarcl.List)
                    {
                        if (respObjCar.Object.Carclass.Id == seasonCarclass.Carclass.Id) { Car = respObjCar.Object; return true; }
                    }
                    if (replyWithError)
                    {
                        LogText = "Es sind keine Fahrzeuge der Klasse " + respObjCar.Object.Carclass.Name + " für die Meisterschaft zugelassen.";
                        await ErrorResponse();
                    }
                    return false;
                }
                if (replyWithError)
                {
                    LogText = "Bitte eine gültige Fahrzeugnummer angeben. Du findest alle Fahrzeugnummern in der `!fahrzeugliste`.";
                    await ErrorResponse();
                }
            }
            return false;
        }

        public static ushort ParseRaceNumber(string input)
        {
            if (ushort.TryParse(input, out ushort output)) { return output; }
            return ushort.MaxValue;
        }

        public static int ParseEventNr(string input)
        {
            if (int.TryParse(input, out int output)) { return output; }
            return int.MinValue;
        }

        public static uint ParseAccCarId(string input)
        {
            if (uint.TryParse(input, out uint output)) { return output; }
            return uint.MaxValue;
        }
    }
}
