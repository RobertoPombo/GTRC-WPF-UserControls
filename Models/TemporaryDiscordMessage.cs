using Newtonsoft.Json;
using System.IO;
using System.Text;

using GTRC_Basics;
using GTRC_WPF;

namespace GTRC_WPF_UserControls.Models
{
    public class TemporaryDiscordMessage
    {
        private static readonly string path = GlobalValues.DataDirectory + "temporary discord messages.json";
        public static List<TemporaryDiscordMessage> List = [];

        public ulong MessageId { get; set; } = GlobalValues.NoDiscordId;
        public ulong ChannelId { get; set; } = GlobalValues.NoDiscordId;
        public DiscordMessageType Type { get; set; } = DiscordMessageType.Commands;
        public bool DoesOverride(TemporaryDiscordMessage message)
        {
            if ((
            message.Type == DiscordMessageType.Commands ||
            message.Type == DiscordMessageType.Events ||
            message.Type == DiscordMessageType.Cars ||
            message.Type == DiscordMessageType.Rating ||
            message.Type == DiscordMessageType.Organizations
            ) && (
            Type == DiscordMessageType.Commands ||
            Type == DiscordMessageType.Entries ||
            Type == DiscordMessageType.NewEntries ||
            Type == DiscordMessageType.EntriesIssues ||
            Type == DiscordMessageType.Events ||
            Type == DiscordMessageType.Cars ||
            Type == DiscordMessageType.Rating ||
            Type == DiscordMessageType.Organizations
            )) { return true; }

            else if ((
            message.Type == DiscordMessageType.Entries
            ) && (
            Type == DiscordMessageType.Entries
            )) { return true; }

            else if ((
            message.Type == DiscordMessageType.EntriesIssues
            ) && (
            Type == DiscordMessageType.EntriesIssues
            )) { return true; }

            return false;
        }

        public bool IsOverridable()
        {
            DiscordMessageType[] listTypes = (DiscordMessageType[])Enum.GetValues(typeof(DiscordMessageType));
            foreach (DiscordMessageType type in listTypes )
            {
                TemporaryDiscordMessage message = new() { Type = type };
                if (message.DoesOverride(this)) { return true; };
            }
            return false;
        }

        public static void LoadJson()
        {
            List.Clear();
            if (!Directory.Exists(GlobalValues.DataDirectory)) { Directory.CreateDirectory(GlobalValues.DataDirectory); }
            if (!File.Exists(path)) { File.WriteAllText(path, JsonConvert.SerializeObject(List, Formatting.Indented), Encoding.Unicode); }
            try { List = JsonConvert.DeserializeObject<List<TemporaryDiscordMessage>>(File.ReadAllText(path, Encoding.Unicode)) ?? []; }
            catch { GlobalValues.CurrentLogText = "Load temporary discord messages failed!"; }
        }

        public static void SaveJson()
        {
            string text = JsonConvert.SerializeObject(List, Formatting.Indented);
            File.WriteAllText(path, text, Encoding.Unicode);
        }
    }
}
