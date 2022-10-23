using Binance.Net.Enums;
using BotLogic.Logger;
using BotLogic.Wrappers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BotLogic.Logic
{
    class Bot : IBot
    {
        public Bot(ITrader trader, LoggerCollection logger)
        {
            Trader = trader;
            this.logger = logger;
        }

        public ITrader Trader { get; }
        private readonly LoggerCollection logger;
        private readonly SemaphoreSlim semaphore = new(1);

        #region Trade managers
        private bool isTradeEnabled;
        public bool IsTradeEnabled
        {
            get => isTradeEnabled;
            set
            {
                if (value)
                {
                    Trader.OnTick += OnTick;
                    Trader.OnTradeTransaction += OnOrderUpdate;
                }
                else
                {
                    Trader.OnTick -= OnTick;
                    Trader.OnTradeTransaction -= OnOrderUpdate;
                }
                isTradeEnabled = value;
            }
        }
        #endregion

        #region Params
        public OrderSide Direction { get; set; }
        public decimal Volume { get; set; }
        public decimal Buffer { get; set; }
        public decimal Distance { get; set; }
        public decimal SL { get; set; }
        public decimal TP { get; set; }
        public string Asset => Trader.Symbol.Name;

        private int stepPrice => getStep("price"); //VR+
        private int stepVolume => getStep("volume"); //VR+
        #endregion

        private int getStep(string PriceVolume)
        {
            string str = "";
            if (PriceVolume == "price")
                str = Trader.Symbol.PriceFilter.TickSize.ToString();
            else
                str = Trader.Symbol.LotSizeFilter.StepSize.ToString();

            string[] arr = str.Split('.');
            if (arr.Length == 2)
                return arr[1].Length;
            else
            {
                arr = str.Split(',');
                if (arr.Length == 2)
                    return arr[1].Length;
                else
                    return 0;
            }
        }
        public bool IsEqual(IBot bot)
        {
            return Direction == bot.Direction &&
                   Volume == bot.Volume &&
                   Buffer == bot.Buffer &&
                   Distance == bot.Distance &&
                   SL == bot.SL &&
                   TP == bot.TP &&
                   Asset == bot.Asset;
        }

        #region Logic
        #region Order keepers

        private TradeDetales _initialOrder;
        private TradeDetales _tpOrder;
        private decimal _slPrice;

        public TradeDetales InitialOrder
        {
            get => _initialOrder;
            set
            {
                _initialOrder = value;
                OrderChanged();
            }
        }
        public TradeDetales TpOrder
        {
            get => _tpOrder;
            set
            {
                _tpOrder = value;
                OrderChanged();
            }
        }
        public decimal SlPrice
        {
            get => _slPrice;
            set
            {
                _slPrice = value;
                OrderChanged();
            }
        }

        public Dictionary<BotOrderType, TradeDetales> Orders
        {
            get
            {
                Dictionary<BotOrderType, TradeDetales> ans = new();
                if (InitialOrder != null)
                    ans.Add(BotOrderType.Initial, InitialOrder);
                if (TpOrder != null)
                    ans.Add(BotOrderType.TP, TpOrder);
                if (SlPrice > 0)
                {
                    ans.Add(BotOrderType.SL, new()
                    {
                        Id = 0,
                        Direction = (Direction == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy),
                        Price = SlPrice,
                        Status = OrderStatus.New,
                        ActualVolume = (InitialOrder != null ? InitialOrder.FilledVolume : 0),
                        FilledVolume = 0
                    });
                }

                return ans;
            }
        }
        #endregion

        private bool skipped = false;

        public event Action OrdersChanged;
        private void OrderChanged()
        {
            OrdersChanged?.Invoke();
        }

        private bool needDelay;
        private async void OnTick(decimal price)
        {
            if (skipped) return;

            await semaphore.WaitAsync();

            try
            {
                skipped = true;

                if (needDelay)
                {
                    await Task.Delay(60000);
                    needDelay = false;
                }

                if (InitialOrder != null && InitialOrder.Status == OrderStatus.New)
                {
                    //if (Direction == OrderSide.Buy ? price > InitialOrder.Border : price < InitialOrder.Border)
                    if (price < InitialOrder.BorderDown || price > InitialOrder.BorderUp)
                    {
                        if ((await Trader.CancelOrder(InitialOrder.Id)) == null)
                        {
                            logger.LogWarning($"{Asset} | Can`t cancel openning order with ID = {InitialOrder.Id}");
                        }
                        else
                        {
                            InitialOrder = null;
                        }
                    }
                }

                if (InitialOrder == null)
                {
                    if (TpOrder != null)
                    {
                        //на всякий пожарный, для подстразовки.
                        logger.LogWarning($"{Asset} | Something went wrong. We have Tp order, but don  t have position. The expert will continue trading and ignore TP order. You have to delete it manualy Id = {TpOrder.Id}.");
                        TpOrder = null;
                        SlPrice = 0;
                    }
                    decimal openVolume = Math.Round(Volume / price, stepVolume); //VR+
                    if (openVolume == 0)
                    {
                        logger.LogWarning($"{Asset} | Volume is zero {Volume / price}");
                        return;
                    }
                    decimal openPrice = Math.Round(price * (Direction == OrderSide.Buy ? (1 - Distance) : (1 + Distance)), stepPrice); //VR+
                    var res = await Trader.SetLimit(openPrice, openVolume, Direction);
                    //var res = await Trader.SetMarket(openVolume, Direction);
                    if (res == null)
                    {
                        logger.LogWarning($"{Asset} | Can`t open limit order");
                        await Task.Delay(10000); // delay 10 seconds
                    }
                    else
                    {
                        InitialOrder = new()
                        {
                            Id = res.OrderId,
                            ActualVolume = res.OriginalQuantity - res.ExecutedQuantity,
                            Direction = Direction,
                            FilledVolume = res.ExecutedQuantity,
                            Price = res.Price,
                            Status = res.Status,
                            BorderDown = price * (Direction == OrderSide.Buy ? (1 - Buffer) : (1 - Buffer - 0.0005m)),
                            BorderUp = price * (Direction == OrderSide.Buy ? (1 + Buffer + 0.0005m) : (1 + Buffer))
                        };
                    }
                }
                else if (InitialOrder.Status == OrderStatus.Filled)
                {
                    if (Direction == OrderSide.Buy ? price <= SlPrice : price >= SlPrice)
                    {
                        var direction = (InitialOrder.Direction == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy);
                        decimal volume = InitialOrder.FilledVolume;

                        if ((await Trader.SetMarket(volume, direction)) == null)
                        {
                            logger.LogWarning($"{Asset} | Can`t close position by SL (try to set market {direction} order with volume = {volume})");
                            await Task.Delay(10000); // delay 10 seconds
                        }
                        else
                        {
                            SlPrice = 0;
                            if (TpOrder != null)
                            {
                                if ((await Trader.CancelOrder(TpOrder.Id)) == null)
                                {
                                    logger.LogWarning($"{Asset} | Can`t cancel TP order with ID = {TpOrder.Id}");
                                }
                                else
                                    TpOrder = null;
                            }
                            InitialOrder = null;
                            await Task.Delay(60000);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Bot | ");
            }
            finally
            {
                skipped = false;
                semaphore.Release();
            }
        }

        private async void OnOrderUpdate(TradeDetales tradeDetales)
        {
            try
            {
                if (TpOrder != null && tradeDetales.Id == TpOrder.Id)
                {
                    if (tradeDetales.Status == OrderStatus.Filled)
                    {
                        await semaphore.WaitAsync();
                        needDelay = true;
                        InitialOrder = null;
                        SlPrice = 0;
                        TpOrder = null;
                        semaphore.Release();
                    }
                    else
                        TpOrder = tradeDetales;
                }
                else if (InitialOrder != null && tradeDetales.Id == InitialOrder.Id)
                {
                    tradeDetales.BorderDown = InitialOrder.BorderDown;
                    tradeDetales.BorderUp = InitialOrder.BorderUp;
                    InitialOrder = tradeDetales;
                    if (tradeDetales.Status == OrderStatus.Filled)
                    {
                        decimal price = tradeDetales.Price * (Direction == OrderSide.Buy ? (1 + TP) : (1 - TP));
                        SlPrice = tradeDetales.Price * (Direction == OrderSide.Buy ? (1 - SL) : (1 + SL));
                        var res = await Trader.SetLimit(price, tradeDetales.FilledVolume, (tradeDetales.Direction == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy));
                        if (res == null)
                        {
                            logger.LogWarning($"{Asset}| Can`t set limit TP order");
                        }
                        else
                        {
                            TpOrder = new()
                            {
                                Id = res.OrderId,
                                Direction = res.Side,
                                Price = res.Price,
                                Status = res.Status,
                                ActualVolume = res.OriginalQuantity - res.ExecutedQuantity,
                                FilledVolume = res.ExecutedQuantity
                            };
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Bot | ");
            }
        }
        #endregion
    }
}
