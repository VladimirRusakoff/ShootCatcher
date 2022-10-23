using Binance.Net.Enums;
using Binance.Net.Objects.Futures.FuturesData;
using BotLogic;
using BotLogic.Logic;
using BotLogic.Wrappers;
using Newtonsoft.Json;
using ShootCatcher.Helpers;
using ShootCatcher.Model;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShootCatcher.View
{
    class MainView : INotifyPropertyChanged
    {
        public MainView()
        {
            model = new(dispatcher);
            model.Logic.OnTradeTransaction += Logic_OnTradeTransaction;
            model.Logic.OnPositionUpdate += Logic_OnPositionUpdate;
            model.Logic.AddedNewBots += Logic_AddedNewBots;
            Connect = new RelayCommand(ConnectBinance);
            Disconnect = new RelayCommand(DisconnectBinance);
            model.AccountDataUpdate += Model_AccountDataUpdate;
            OpenAddnewBotManager = new RelayCommand(o =>
            {
                AddStratagyWindowHelper.Open();
            });
            botSettings = BotSettings.InstanceOrDeserialize(BOT_SETTINGS_NAME);
            StartAll = new RelayCommand(o => StartNotStarted());
            StopAll = new RelayCommand(o => StopStarted());
        }

        private void Logic_OnPositionUpdate(System.Collections.Generic.List<PositionInfo> activePositions)
        {
            dispatcher.Invoke(() =>
            {
                PositionsInfo.Clear();
                foreach (var item in activePositions)
                {
                    PositionsInfo.Add(item);
                }
            });
        }

        private void Logic_OnTradeTransaction(TradeDetales order)
        {
            dispatcher.Invoke(() =>
            {
                var selectedOrder = Orders.FirstOrDefault(x => x.Id == order.Id);
                if (selectedOrder == null)
                {
                    Orders.Add(order);
                }
                else
                {
                    Orders.Remove(selectedOrder);
                    if (order.Status == OrderStatus.New || order.Status == OrderStatus.PartiallyFilled || order.Status == OrderStatus.PendingCancel)
                    {
                        Orders.Add(order);
                    }
                }
            });
        }

        private const string BOT_SETTINGS_NAME = "settings.json";
        private readonly BotSettings botSettings = new();

        private void Logic_AddedNewBots(System.Collections.Generic.List<IBot> newBots)
        {
            dispatcher?.Invoke(() =>
            {
                foreach (var bot in newBots)
                {
                    var setting = new Settings
                    {
                        Asset = bot.Asset,
                        Buffer = bot.Buffer * 200,
                        Direction = bot.Direction,
                        Distance = bot.Distance * 100,
                        SL = bot.SL * 100,
                        TP = bot.TP * 100,
                        Volume = bot.Volume
                    };
                    if (!botSettings.Settings.Any(x => x.Compare(setting)))
                        botSettings.Settings.Add(setting);

                    Bots.Add(new(bot, item =>
                    {
                        Bots.Remove(item);
                        botSettings.Settings.RemoveAll(x =>
                        {
                            bool asset = x.Asset == item.Bot.Asset;
                            bool buffer = x.Buffer == item.Bot.Buffer * 200;
                            bool direction = x.Direction == item.Bot.Direction;
                            bool distance = x.Distance == item.Bot.Distance * 100;
                            bool sl = x.SL == item.Bot.SL * 100;
                            bool tp = x.TP == item.Bot.TP * 100;
                            bool volume = x.Volume == item.Bot.Volume;

                            return asset && buffer && direction && distance && sl && tp && volume;
                        });
                    }, model.Logic));
                }
            });
        }

        private void Model_AccountDataUpdate()
        {
            dispatcher.Invoke(() =>
            {
                AccountInfo.Clear();
                AccountInfo.Add(model.AccountData);
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            if (dispatcher.Thread != Thread.CurrentThread)
                dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)));
            else
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private readonly MainWindowLogic model;
        private readonly System.Windows.Threading.Dispatcher dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        public ObservableCollection<BinanceFuturesAccountInfo> AccountInfo { get; } = new();
        public ObservableCollection<PositionInfo> PositionsInfo { get; } = new();
        public ObservableCollection<TradeDetales> Orders { get; } = new();
        public ObservableCollection<BotKeeper> Bots { get; } = new();
        public ObservableCollection<LogItem> LogsKeeper => model.LogsKeeper;
      
        #region Connect / Disconnect
        public string ApiKey
        {
            get => botSettings.APIkey;
            set => botSettings.APIkey = value;
        }
        public string ApiSecretKey
        {
            get => botSettings.Seckey;
            set => botSettings.Seckey = value;
        }


        private string connectionStatus = "Disconnected";
        public string ConnectionStatus
        {
            get => connectionStatus;
            set
            {
                connectionStatus = value;
                OnPropertyChanged("ConnectionStatus");
            }
        }
        private bool Connected => model.Logic.ConnectionStatus;
        public bool IsConnectBtnEnabled => !Connected && !tryToReconnect;
        public bool ConnectedBtnEnabled => Connected && !tryToReconnect;
        private bool tryToReconnect;
        public ICommand Connect { get; }
        async void ConnectBinance(object o)
        {
            await Task.Run(async () =>
            {

                tryToReconnect = true;
                OnPropertyChanged("IsConnectBtnEnabled");
                ConnectionStatus = "Connecting...";
                try
                {
                    botSettings.Serialization(BOT_SETTINGS_NAME);
                    if (ApiKey == "" || ApiSecretKey == "")
                        return;
                    await model.Subscribe(ApiKey, ApiSecretKey);
                    dispatcher.Invoke(() =>
                    {
                        Orders.Clear();
                        PositionsInfo.Clear();
                    });
                    if (botSettings.Settings.Count > 0)
                    {
                        foreach (var setting in botSettings.Settings)
                        {
                            model.Logic.AddNewBot(setting.Asset, setting.Direction, setting.Volume, setting.Buffer, setting.Distance, setting.SL, setting.TP);
                        }
                    }
                }
                finally
                {
                    tryToReconnect = false;
                    OnPropertyChanged("AvalvbleAssets");
                    ConnectionStatus = Connected ? "Connected" : "Disconnected";
                    OnPropertyChanged("ConnectedBtnEnabled");
                    OnPropertyChanged("AvalvbleAssets");
                    OnPropertyChanged("SelectedAsset");
                    OnPropertyChanged("IsConnectBtnEnabled");
                }
            });
        }

        public ICommand Disconnect { get; }
        async void DisconnectBinance(object o)
        {
            await Task.Run(async () =>
            {
                tryToReconnect = true;
                OnPropertyChanged("ConnectedBtnEnabled");
                ConnectionStatus = "Disconnecting...";
                try
                {
                    botSettings.Serialization(BOT_SETTINGS_NAME);
                    await model.Unsubscribe();
                    dispatcher.Invoke(() =>
                    {
                        Bots.Clear();
                    });
                }
                finally
                {
                    tryToReconnect = false;
                    AddStratagyWindowHelper.Close();
                    ConnectionStatus = Connected ? "Connected" : "Disconnected";
                    dispatcher.Invoke(() =>
                    {
                        PositionsInfo.Clear();
                        Orders.Clear();
                        AccountInfo.Clear();
                    });
                    OnPropertyChanged("ConnectedBtnEnabled");
                    OnPropertyChanged("IsConnectBtnEnabled");
                }

            });
        }
        #endregion

        #region Stratagy
        public ICommand OpenAddnewBotManager { get; }
        public ICommand StartAll { get; }
        void StartNotStarted()
        {
            foreach (var bot in Bots)
            {
                if (!bot.Bot.IsTradeEnabled)
                    bot.Start.Execute(null);
            }
        }
        public ICommand StopAll { get; }
        void StopStarted()
        {
            foreach (var bot in Bots)
            {
                if (bot.Bot.IsTradeEnabled)
                    bot.Stop.Execute(null);
            }
        }
        #endregion

    }
}
