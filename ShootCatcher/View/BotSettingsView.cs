using ShootCatcher.Model;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ShootCatcher.View
{
    class BotSettingsView
    {
        public BotSettingsView()
        {
            AddAsset = new RelayCommand(o =>
            {
                if (!string.IsNullOrEmpty(model.SelectedFut) && !string.IsNullOrWhiteSpace(model.SelectedFut) &&
                    !model.SelectedAssets.Any(x => x.Asset == model.SelectedFut))
                {
                    model.SelectedAssets.Add(new AssetItem(model.SelectedFut, model));
                }
            });
        }

        private readonly BotSettingsLogic model = new();

        public string[] Futures => model.AvalableFutures;
        public string SelectedFuturs
        {
            get => model.SelectedFut;
            set => model.SelectedFut = value;
        }

        public ObservableCollection<AssetItem> SelectedAssets => model.SelectedAssets;
        public ICommand AddAsset { get; }
        public bool IsLong
        {
            get => model.Direction == Binance.Net.Enums.OrderSide.Buy;
            set => model.Direction = value ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell;
        }

        public decimal Volume
        {
            get => model.Volume;
            set
            {
                if (Volume > 0)
                {
                    model.Volume = value;
                }
            }
        }

        public decimal Buffer
        {
            get => model.Buffer;
            set
            {
                if (value > 0 && value <= model.Distance)
                {
                    model.Buffer = value;
                }
            }
        }
        public decimal Distance
        {
            get => model.Distance;
            set
            {
                if (value > 0 && value >= model.Buffer)
                {
                    model.Distance = value;
                }
            }
        }
        public decimal SL
        {
            get => model.SL;
            set
            {
                if (value > 0)
                {
                    model.SL = value;
                }
            }
        }
        public decimal TP
        {
            get => model.TP;
            set
            {
                if (value > 0)
                {
                    model.TP = value;
                }
            }
        }
        public ICommand SaveChanges => new RelayCommand(o => model.SaveChanges());
    }
}
