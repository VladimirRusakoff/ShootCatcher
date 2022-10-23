using Binance.Net.Enums;
using BotLogic.Wrappers;
using System;
using System.Collections.Generic;

namespace BotLogic.Logic
{
    public interface IBot
    {
        public bool IsTradeEnabled { get; }
        public OrderSide Direction { get; set; }
        public decimal Volume { get; set; }
        public decimal Buffer { get; set; }
        public decimal Distance { get; set; }
        public decimal SL { get; set; }
        public decimal TP { get; set; }
        public string Asset { get; }
        public bool IsEqual(IBot bot);
        public Dictionary<BotOrderType, TradeDetales> Orders { get; }
        public event Action OrdersChanged;
    }
    public enum BotOrderType
    {
        SL,
        TP,
        Initial
    }
}
