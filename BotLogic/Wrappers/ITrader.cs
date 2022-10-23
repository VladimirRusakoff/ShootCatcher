using Binance.Net.Enums;
using Binance.Net.Objects.Futures.FuturesData;
using Binance.Net.Objects.Futures.MarketData;
using System;
using System.Threading.Tasks;

namespace BotLogic.Wrappers
{
    interface ITrader
    {
        BinanceFuturesUsdtSymbol Symbol { get; }
        event Action<TradeDetales> OnTradeTransaction;
        event Action<decimal> OnTick;
        Task<BinanceFuturesCancelOrder> CancelOrder(long orderId);
        Task<BinanceFuturesPlacedOrder> SetLimit(decimal price, decimal volume, OrderSide direction);
        Task<BinanceFuturesPlacedOrder> SetMarket(decimal volume, OrderSide direction);
    }

    public class TradeDetales
    {
        public string Symbol { get; set; }
        public long Id { get; set; }
        public decimal FilledVolume { get; set; }
        public decimal ActualVolume { get; set; }
        public decimal Price { get; set; }
        public OrderSide Direction { get; set; }
        public OrderStatus Status { get; set; }
        public decimal BorderDown { get; set; }
        public decimal BorderUp { get; set; }
    }
}
