using System.Collections.ObjectModel;
using System.Windows;

using GTRC_Basics;
using GTRC_Database_Client;
using GTRC_WPF;

namespace GTRC_WPF_UserControls.ViewModels
{
    public class DbApiConnectionConfigVM : ObservableObject
    {
        private DbApiConnectionConfig? selected;

        public DbApiConnectionConfigVM()
        {
            RestoreJsonCmd = new UICmd((o) => RestoreJson());
            SaveJsonCmd = new UICmd((o) => DbApiConnectionConfig.SaveJson());
            AddPresetCmd = new UICmd((o) => AddPreset());
            DelPresetCmd = new UICmd((o) => DelPreset());
            RestoreJson();
        }

        public string Name { get; set; } = "Database API";

        public ObservableCollection<DbApiConnectionConfig> List
        {
            get { ObservableCollection<DbApiConnectionConfig> _list = []; foreach (DbApiConnectionConfig con in DbApiConnectionConfig.List) { _list.Add(con); } return _list; }
            set { }
        }

        public ObservableCollection<ProtocolType> ProtocolTypes
        {
            get { ObservableCollection<ProtocolType> _list = []; foreach (ProtocolType type in Enum.GetValues(typeof(ProtocolType))) { _list.Add(type); } return _list; }
            set { }
        }

        public ObservableCollection<NetworkType> NetworkTypes
        {
            get { ObservableCollection<NetworkType> _list = []; foreach (NetworkType type in Enum.GetValues(typeof(NetworkType))) { _list.Add(type); } return _list; }
            set { }
        }

        public ObservableCollection<IpAdressType> IpAdressTypes
        {
            get { ObservableCollection<IpAdressType> _list = []; foreach (IpAdressType type in Enum.GetValues(typeof(IpAdressType))) { _list.Add(type); } return _list; }
            set { }
        }

        public DbApiConnectionConfig? Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(NetworkType));
                RaisePropertyChanged(nameof(IpAdressType));
                RaisePropertyChanged(nameof(IsActive));
                UpdateAllVisibilities();
            }
        }

        public NetworkType NetworkType
        {
            get { return Selected?.NetworkType ?? NetworkType.IpAdress; }
            set
            {
                if (Selected is not null) { Selected.NetworkType = value; }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(VisibilityIpAdressType));
                RaisePropertyChanged(nameof(VisibilityIpv4));
                RaisePropertyChanged(nameof(VisibilityIpv6));
                RaisePropertyChanged(nameof(VisibilityPort));
                RaisePropertyChanged(nameof(VisibilityPcName));
                RaisePropertyChanged(nameof(VisibilityUserId));
                RaisePropertyChanged(nameof(VisibilityPassword));
            }
        }

        public IpAdressType IpAdressType
        {
            get { return Selected?.IpAdressType ?? IpAdressType.IPv4; }
            set
            {
                if (Selected is not null) { Selected.IpAdressType = value; }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(VisibilityIpv4));
                RaisePropertyChanged(nameof(VisibilityIpv6));
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
                    ConfirmActiveConnection();
                    RaisePropertyChanged();
                }
            }
        }

        public Visibility VisibilityName { get { return Visibility.Visible; } }

        public Visibility VisibilityProtocolType { get { return Visibility.Visible; } }

        public Visibility VisibilityNetworkType { get { return Visibility.Visible; } }

        public Visibility VisibilityIpAdressType
        {
            get
            {
                if (Selected?.NetworkType != NetworkType.Localhost) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

        public Visibility VisibilityIpv4
        {
            get
            {
                if (VisibilityIpAdressType == Visibility.Visible && Selected?.IpAdressType == IpAdressType.IPv4) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

        public Visibility VisibilityIpv6
        {
            get
            {
                if (VisibilityIpAdressType != VisibilityIpv4) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

        public Visibility VisibilityPort { get { return Visibility.Visible; } }

        public Visibility VisibilitySourceName { get { return Visibility.Collapsed; } }

        public Visibility VisibilityCatalogName { get { return Visibility.Collapsed; } }

        public Visibility VisibilityPcName { get { return Visibility.Collapsed; } }

        public Visibility VisibilityUserId { get { return Visibility.Collapsed; } }

        public Visibility VisibilityPassword { get { return Visibility.Collapsed; } }

        public void UpdateAllVisibilities()
        {
            RaisePropertyChanged(nameof(VisibilityName));
            RaisePropertyChanged(nameof(VisibilityProtocolType));
            RaisePropertyChanged(nameof(VisibilityNetworkType));
            RaisePropertyChanged(nameof(VisibilityIpAdressType));
            RaisePropertyChanged(nameof(VisibilityIpv4));
            RaisePropertyChanged(nameof(VisibilityIpv6));
            RaisePropertyChanged(nameof(VisibilityPort));
            RaisePropertyChanged(nameof(VisibilitySourceName));
            RaisePropertyChanged(nameof(VisibilityCatalogName));
            RaisePropertyChanged(nameof(VisibilityPcName));
            RaisePropertyChanged(nameof(VisibilityUserId));
            RaisePropertyChanged(nameof(VisibilityPassword));
        }

        public DbApiConnectionConfig? ConfirmActiveConnection()
        {
            OnConfirmApiConnectionEstablished();
            DbApiConnectionConfig? activeCon = DbApiConnectionConfig.GetActiveConnection();
            if (activeCon is null) { GlobalValues.CurrentLogText = "Not connected to GTRC-Database-API."; }
            else { GlobalValues.CurrentLogText = "Connection to GTRC-Database-API succeded."; }
            return activeCon;
        }

        public void RestoreJson()
        {
            DbApiConnectionConfig.LoadJson();
            RaisePropertyChanged(nameof(List));
            DbApiConnectionConfig? activeCon = ConfirmActiveConnection();
            if (activeCon is null) { Selected = DbApiConnectionConfig.List[0]; } else { Selected = activeCon; }
        }

        public void AddPreset()
        {
            DbApiConnectionConfig con = new();
            RaisePropertyChanged(nameof(List));
            Selected = con;
        }

        public void DelPreset()
        {
            if (Selected is not null && DbApiConnectionConfig.List.Count > 1 && !Selected.IsActive)
            {
                DbApiConnectionConfig.List.Remove(Selected);
                RaisePropertyChanged(nameof(List));
                Selected = DbApiConnectionConfig.List[0];
            }
        }

        public static event Notify? ConfirmApiConnectionEstablished;

        public static void OnConfirmApiConnectionEstablished() { ConfirmApiConnectionEstablished?.Invoke(); }

        public UICmd RestoreJsonCmd { get; set; }
        public UICmd SaveJsonCmd { get; set; }
        public UICmd AddPresetCmd { get; set; }
        public UICmd DelPresetCmd { get; set; }
    }
}
