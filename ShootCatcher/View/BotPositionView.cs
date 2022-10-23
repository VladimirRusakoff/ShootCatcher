using Binance.Net.Enums;
using BotLogic.Logic;
using BotLogic.Wrappers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;

namespace ShootCatcher.View
{
    class BotPositionView : INotifyPropertyChanged
    {
        private IBot bot;

        public void SetBot(IBot bot)
        {
            this.bot = bot;
            OnPropertyChanged("Direction");
            OnPropertyChanged("Volume");
            OnPropertyChanged("Buffer");
            OnPropertyChanged("TP");
            OnPropertyChanged("SL");
            OnPropertyChanged("Asset");

            this.bot.OrdersChanged += Bot_OrdersChanged;
            Bot_OrdersChanged();
        }

        private void Bot_OrdersChanged()
        {
            dispatcher?.Invoke(() =>
            {
                Orders.Clear();
                var orders = bot.Orders;
                foreach (var order in orders)
                {
                    Orders.Add(new(order.Value, order.Key));
                }
            });
        }

        private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new(name));
        }

        public void Unsubscribe()
        {
            if (bot != null)
                bot.OrdersChanged -= Bot_OrdersChanged;
        }

        public OrderSide Direction => bot != null ? bot.Direction : OrderSide.Buy;
        public decimal Volume => bot != null ? bot.Volume : 0;
        public decimal Buffer => bot != null ? bot.Buffer : 0;
        public decimal TP => bot != null ? bot.TP : 0;
        public decimal SL => bot != null ? bot.SL : 0;
        public string Asset => bot != null ? bot.Asset : "";

        public ObservableCollection<BotOrder> Orders { get; } = new();

    }

    class BotOrder : TradeDetales
    {
        public BotOrder(TradeDetales order, BotOrderType type)
        {
            OrderType = type;
            Id = order.Id;
            FilledVolume = order.FilledVolume;
            ActualVolume = order.ActualVolume;
            Price = order.Price;
            Direction = order.Direction;
            Status = order.Status;
        }

        public BotOrderType OrderType { get; }
    }
}
