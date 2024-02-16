using System.Collections.ObjectModel;
using System.Windows;

using GTRC_Basics;
using GTRC_WPF;

namespace GTRC_WPF_UserControls.ViewModels
{
    public class SqlConnectionConfigVM : ObservableObject
    {
        private SqlConnectionConfig? selected;

        public SqlConnectionConfigVM()
        {
            RestoreJsonCmd = new UICmd((o) => RestoreJson());
            SaveJsonCmd = new UICmd((o) => SqlConnectionConfig.SaveJson());
            AddPresetCmd = new UICmd((o) => AddPreset());
            DelPresetCmd = new UICmd((o) => DelPreset());
            RestoreJson();
        }

        public string Name { get; set; } = "SQL Database";

        public ObservableCollection<SqlConnectionConfig> List
        {
            get { ObservableCollection<SqlConnectionConfig> _list = []; foreach (SqlConnectionConfig conSet in SqlConnectionConfig.List) { _list.Add(conSet); } return _list; }
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

        public SqlConnectionConfig? Selected
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

        public Visibility VisibilityProtocolType { get { return Visibility.Collapsed; } }

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

        public Visibility VisibilityPort
        {
            get
            {
                if (Selected?.NetworkType == NetworkType.IpAdress) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

        public Visibility VisibilitySourceName { get { return Visibility.Visible; } }

        public Visibility VisibilityCatalogName { get { return Visibility.Visible; } }

        public Visibility VisibilityPcName
        {
            get
            {
                if (Selected?.NetworkType == NetworkType.Localhost) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

        public Visibility VisibilityUserId
        {
            get
            {
                if (Selected?.NetworkType == NetworkType.IpAdress) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

        public Visibility VisibilityPassword
        {
            get
            {
                if (Selected?.NetworkType == NetworkType.IpAdress) { return Visibility.Visible; }
                else { return Visibility.Collapsed; }
            }
        }

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

        public SqlConnectionConfig? ConfirmActiveConnection()
        {
            OnConfirmSqlConnectionEstablished();
            SqlConnectionConfig? activeCon = SqlConnectionConfig.GetActiveConnection();
            if (activeCon is null) { GlobalValues.CurrentLogText = "Not connected to SQL-Database."; }
            else if (activeCon.Connectivity()) { GlobalValues.CurrentLogText = "Connection to SQL-Database succeded."; }
            else { GlobalValues.CurrentLogText = "Connection to SQL-Database failed!"; }
            return activeCon;
        }

        public void RestoreJson()
        {
            SqlConnectionConfig.LoadJson();
            RaisePropertyChanged(nameof(List));
            SqlConnectionConfig? activeConSet = ConfirmActiveConnection();
            if (activeConSet is null) { Selected = SqlConnectionConfig.List[0]; } else { Selected = activeConSet; }
        }

        public void AddPreset()
        {
            SqlConnectionConfig newConSet = new();
            RaisePropertyChanged(nameof(List));
            Selected = newConSet;
        }

        public void DelPreset()
        {
            if (Selected is not null && SqlConnectionConfig.List.Count > 1 && !Selected.IsActive)
            {
                SqlConnectionConfig.List.Remove(Selected);
                RaisePropertyChanged(nameof(List));
                Selected = SqlConnectionConfig.List[0];
            }
        }

        public static event Notify? ConfirmSqlConnectionEstablished;

        public static void OnConfirmSqlConnectionEstablished() { ConfirmSqlConnectionEstablished?.Invoke(); }

        public UICmd RestoreJsonCmd { get; set; }
        public UICmd SaveJsonCmd { get; set; }
        public UICmd AddPresetCmd { get; set; }
        public UICmd DelPresetCmd { get; set; }
    }
}
