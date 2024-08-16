using System.Collections.ObjectModel;

using GTRC_Basics;
using GTRC_Basics.Configs;
using GTRC_WPF;

namespace GTRC_WPF_UserControls.ViewModels
{
    public class DiscordBotConfigVM : ObservableObject
    {
        private DiscordBotConfig? selected;

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
            set { selected = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(IsActive)); }
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
