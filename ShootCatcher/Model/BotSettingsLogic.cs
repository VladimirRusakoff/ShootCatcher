using Binance.Net.Enums;
using ShootCatcher.View;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ShootCatcher.Model
{
    class BotSettingsLogic
    {
        public BotSettingsLogic()
        {
            AvalableFutures = manager.Futures;
            SelectedFut = AvalableFutures.Length > 0 ? AvalableFutures[0] : string.Empty;
        }

        private static readonly BotLogicManager manager = BotLogicManager.Instance(null);
        #region Assets
        public string[] AvalableFutures { get; set; }
        public string SelectedFut { get; set; }
        public ObservableCollection<AssetItem> SelectedAssets { get; } = new();
        #endregion

        public OrderSide Direction { get; set; }
        public decimal Volume { get; set; } = 20m; //VR+
        public decimal Buffer { get; set; } = 0.4m;
        public decimal Distance { get; set; } = 0.6m;
        public decimal SL { get; set; } = 0.5m;
        public decimal TP { get; set; } = 0.5m;

        public void SaveChanges()
        {
            manager.AddNewBot(this);
        }
    }

    class AssetItem
    {
        public AssetItem(string asset, BotSettingsLogic settingKeeper)
        {
            Asset = asset;
            DeleteAsset = new RelayCommand(o => settingKeeper.SelectedAssets.Remove(this));
        }
        public string Asset { get; }
        public ICommand DeleteAsset { get; }
    }
}
