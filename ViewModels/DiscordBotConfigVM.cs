using System.Collections.ObjectModel;
using System.Net;

using GTRC_Basics;
using GTRC_Basics.Configs;
using GTRC_Basics.Models;
using GTRC_Database_Client.Responses;
using GTRC_Database_Client;
using GTRC_WPF;

namespace GTRC_WPF_UserControls.ViewModels
{
    public class DiscordBotConfigVM : ObservableObject
    {
        private DiscordBotConfig? selected;
        private Series? series;
        private Season? season;

        public DiscordBotConfigVM()
        {
            RestoreJsonCmd = new UICmd((o) => RestoreJson());
            SaveJsonCmd = new UICmd((o) => DiscordBotConfig.SaveJson());
            AddPresetCmd = new UICmd((o) => AddPreset());
            DelPresetCmd = new UICmd((o) => DelPreset());
            RestoreJson();
        }

        public ObservableCollection<DiscordBotConfig> List
        {
            get { ObservableCollection<DiscordBotConfig> _list = []; foreach (DiscordBotConfig bot in DiscordBotConfig.List) { _list.Add(bot); } return _list; }
            set { }
        }

        public DiscordBotConfig? Selected
        {
            get { return selected; }
            set { selected = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsActive)); _ = GetListSeries(); }
        }

        public ObservableCollection<Series> ListSeries { get; set; } = [];

        public ObservableCollection<Season> ListSeasons { get; set; } = [];

        public Series? Series
        {
            get { return series; }
            set
            {
                if (series?.Id != value?.Id)
                {
                    series = value;
                    if (selected is not null && series is not null && selected.SeriesId != series.Id) { selected.SeriesId = series.Id; }
                    _ = GetListSeasons();
                }
                RaisePropertyChanged();
            }
        }

        public Season? Season
        {
            get { return season; }
            set
            {
                if (season?.Id != value?.Id)
                {
                    season = value;
                    if (selected is not null && season is not null && selected.SeasonId != season.Id) { selected.SeasonId = season.Id; }
                }
                RaisePropertyChanged();
            }
        }

        public bool IsActive
        {
            get { return Selected?.IsActive ?? false; }
            set
            {
                if (Selected is not null && value != Selected.IsActive)
                {
                    Selected.IsActive = value;
                    ConfirmActiveBot();
                    RaisePropertyChanged();
                }
            }
        }

        public DiscordBotConfig? ConfirmActiveBot()
        {
            OnChangedDiscordBotIsActive();
            DiscordBotConfig? activeBot = DiscordBotConfig.GetActiveBot();
            if (activeBot is null) { GlobalValues.CurrentLogText = "Discord bot stopped."; }
            else { GlobalValues.CurrentLogText = "Discord bot is running."; }
            return activeBot;
        }

        public async Task GetListSeries()
        {
            DbApiListResponse<Series> response = await DbApi.DynConnection.Series.GetAll();
            Series = null;
            ListSeries = [];
            if (response.Status == HttpStatusCode.OK)
            {
                foreach (Series obj in response.List) { ListSeries.Add(obj); }
            }
            RaisePropertyChanged(nameof(ListSeries));
            foreach (Series obj in ListSeries) { if (obj.Id == selected?.SeriesId) { Series = obj; } }
            if (Series is null && ListSeries.Count > 0) { Series = ListSeries[0]; }
        }

        public async Task GetListSeasons()
        {
            Season = null;
            ListSeasons = [];
            if (Series is not null)
            {
                DbApiListResponse<Season> response = await DbApi.DynConnection.Season.GetChildObjects(typeof(Series), Series.Id);
                Season = null;
                ListSeasons = [];
                if (response.Status == HttpStatusCode.OK)
                {
                    foreach (Season obj in response.List) { ListSeasons.Add(obj); }
                }
            }
            RaisePropertyChanged(nameof(ListSeasons));
            foreach (Season obj in ListSeasons) { if (obj.Id == selected?.SeasonId) { Season = obj; } }
            if (Season is null && ListSeasons.Count > 0) { Season = ListSeasons[0]; }
        }

        public void RestoreJson()
        {
            DiscordBotConfig.LoadJson();
            RaisePropertyChanged(nameof(List));
            DiscordBotConfig? activeBot = ConfirmActiveBot();
            if (activeBot is null) { Selected = DiscordBotConfig.List[0]; } else { Selected = activeBot; }
        }

        public void AddPreset()
        {
            DiscordBotConfig newBot = new();
            RaisePropertyChanged(nameof(List));
            Selected = newBot;
        }

        public void DelPreset()
        {
            if (Selected is not null && DiscordBotConfig.List.Count > 1 && !Selected.IsActive)
            {
                DiscordBotConfig.List.Remove(Selected);
                RaisePropertyChanged(nameof(List));
                Selected = DiscordBotConfig.List[0];
            }
        }

        public static event Notify? ChangedDiscordBotIsActive;

        public static void OnChangedDiscordBotIsActive() { ChangedDiscordBotIsActive?.Invoke(); }

        public UICmd RestoreJsonCmd { get; set; }
        public UICmd SaveJsonCmd { get; set; }
        public UICmd AddPresetCmd { get; set; }
        public UICmd DelPresetCmd { get; set; }
    }
}
