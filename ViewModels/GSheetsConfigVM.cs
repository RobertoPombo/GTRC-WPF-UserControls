using System.Collections.ObjectModel;

using GTRC_Basics;
using GTRC_Basics.Configs;
using GTRC_WPF;

namespace GTRC_WPF_UserControls.ViewModels
{
    public class GSheetsConfigVM : ObservableObject
    {
        private GSheetsConfig? selected;

        public GSheetsConfigVM()
        {
            RestoreJsonCmd = new UICmd((o) => RestoreJson());
            SaveJsonCmd = new UICmd((o) => GSheetsConfig.SaveJson());
            AddPresetCmd = new UICmd((o) => AddPreset());
            DelPresetCmd = new UICmd((o) => DelPreset());
            RestoreJson();
        }

        public ObservableCollection<GSheetsConfig> List
        {
            get { ObservableCollection<GSheetsConfig> _list = []; foreach (GSheetsConfig gSheet in GSheetsConfig.List) { _list.Add(gSheet); } return _list; }
            set { }
        }

        public GSheetsConfig? Selected
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
                    ConfirmActiveConnection();
                    RaisePropertyChanged();
                }
            }
        }

        public GSheetsConfig? ConfirmActiveConnection()
        {
            OnConfirmGSheetConnectionEstablished();
            GSheetsConfig? activeCon = GSheetsConfig.GetActiveConnection();
            if (activeCon is null) { GlobalValues.CurrentLogText = "Not connected to Google-Sheets."; }
            else if (activeCon.Connectivity()) { GlobalValues.CurrentLogText = "Connection to Google-Sheets succeded."; }
            else { GSheetsConfig.SetLogTextCredentialsFailure(); }
            return activeCon;
        }

        public void RestoreJson()
        {
            GSheetsConfig.LoadJson();
            RaisePropertyChanged(nameof(List));
            GSheetsConfig? activeConSet = ConfirmActiveConnection();
            if (activeConSet is null) { Selected = GSheetsConfig.List[0]; } else { Selected = activeConSet; }
        }

        public void AddPreset()
        {
            GSheetsConfig newConSet = new(true);
            RaisePropertyChanged(nameof(List));
            Selected = newConSet;
        }

        public void DelPreset()
        {
            if (Selected is not null && GSheetsConfig.List.Count > 1 && !Selected.IsActive)
            {
                GSheetsConfig.List.Remove(Selected);
                RaisePropertyChanged(nameof(List));
                Selected = GSheetsConfig.List[0];
            }
        }

        public static event Notify? ConfirmGSheetConnectionEstablished;

        public static void OnConfirmGSheetConnectionEstablished() { ConfirmGSheetConnectionEstablished?.Invoke(); }

        public UICmd RestoreJsonCmd { get; set; }
        public UICmd SaveJsonCmd { get; set; }
        public UICmd AddPresetCmd { get; set; }
        public UICmd DelPresetCmd { get; set; }
    }
}
