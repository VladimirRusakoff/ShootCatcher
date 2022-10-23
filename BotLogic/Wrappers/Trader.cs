using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Futures.FuturesData;
using Binance.Net.Objects.Futures.MarketData;
using Binance.Net.Objects.Futures.UserStream;
using BotLogic.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BotLogic.Wrappers
{
    class Trader : ITrader
    {
        public Trader(BinanceClient client, BinanceSocketClient soketClient, Logger.LoggerCollection logger, string asset, Manager manager)
        {
            this.client = client;
            this.logger = logger;
            var exchangeinfo = client.FuturesUsdt.System.GetExchangeInfo();
            if (!exchangeinfo.Success)
            {
                throw new Exception("Can`t get exchange info");
            }
            Symbol = exchangeinfo.Data.Symbols.FirstOrDefault(x => x.Name == asset);
            if (Symbol == null)
            {
                throw new Exception("Can`t get symbol");
            }
            var subscription = soketClient.FuturesUsdt.SubscribeToSymbolTickerUpdates(Symbol.Name, tickMessage =>
            {
                if (tickMessage.Symbol == Symbol.Name)
                    OnTick?.Invoke(tickMessage.LastPrice);
            });
            if (!subscription.Success)
            {
                throw new Exception($"Can`t subscribe to the quotes of the sybmol: {Symbol.Name}");
            }
            manager.OnTradeTransaction += OnOrderUpdate;
        }

        private readonly Logger.LoggerCollection logger;
        private readonly BinanceClient client;
        public BinanceFuturesUsdtSymbol Symbol { get; }

        private void OnOrderUpdate(TradeDetales orderUpdate)
        {
            if (orderUpdate.Symbol == Symbol.Name)
            {
                OnTradeTransaction?.Invoke(orderUpdate);
            }
        }

        public event Action<TradeDetales> OnTradeTransaction;
        public event Action<decimal> OnTick;

        private decimal NormalizePrice(decimal price)
        {
            var ans = Normalize(price, Symbol.PriceFilter.TickSize);
            if (ans < Symbol.PriceFilter.MinPrice)
                ans = Symbol.PriceFilter.MinPrice;
            if (ans > Symbol.PriceFilter.MaxPrice)
                ans = Symbol.PriceFilter.MaxPrice;
            return ans;
        }
        private decimal NormalizeLot(decimal lot)
        {
            var ans = Normalize(lot, Symbol.LotSizeFilter.StepSize);
            var assetPrice = client.FuturesUsdt.Market.GetPrice(Symbol.Name);
            if (assetPrice.Success)
            {
                decimal _price = assetPrice.Data.Price;
                if ((ans * _price) >= 5)
                    return ans;
                else
                    ans = Normalize(5 / _price, Symbol.PriceFilter.TickSize);
                if (ans < Symbol.LotSizeFilter.MinQuantity)
                    ans = Symbol.LotSizeFilter.MinQuantity;
                if (ans > Symbol.LotSizeFilter.MaxQuantity)
                    ans = Symbol.LotSizeFilter.MaxQuantity;
                return ans;
            }
            return 0;
        }

        private static decimal Normalize(decimal value, decimal step) => Math.Round(value / step, 0) * step;

        private async Task<BinanceFuturesCancelAllOrders> CanclAllOrders()
        {
            try
            {
                var orders = await client.FuturesUsdt.Order.GetOpenOrdersAsync(Symbol.Name);
                if (orders.Success && orders.Data.Any())
                {
                    var res = await client.FuturesUsdt.Order.CancelAllOrdersAsync(symbol: Symbol.Name);
                    if (res.Success)
                    {
                        return res.Data;
                    }
                    else
                    {
                        logger.LogError($"Error: {res.Error.Code} | Message: {res.Error.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to set limit order");
            }
            return null;
        }

        public async Task CloseAll()
        {
            try
            {
                if ((await CanclAllOrders()) == null)
                {
                    logger.LogWarning($"{Symbol.Name} | Can`t cancel all orders for the symbol, it can be happned if you don`t have open orders at this moment, other side, you have to close it manualy.");
                }
                var accountinfo = client.FuturesUsdt.Account.GetAccountInfo();
                if (accountinfo.Success)
                {
                    var position = accountinfo.Data.Positions.Where(x => x.PositionAmount != 0 && x.Symbol == Symbol.Name);
                    if (position.Any())
                    {
                        foreach (var positionItem in position)
                        {
                            if (positionItem.PositionAmount != 0)
                            {
                                var direction = positionItem.PositionAmount > 0 ? OrderSide.Sell : OrderSide.Buy;
                                var close = SetMarket(Math.Abs(positionItem.PositionAmount), direction);
                                if (close == null)
                                {
                                    logger.LogWarning($"{Symbol.Name} | Can`t close position for the symbol.");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{Symbol.Name} | Error while closing all orders and positions for the symbol");
            }
        }

        public async Task<BinanceFuturesCancelOrder> CancelOrder(long orderId)
        {
            try
            {
                var res = await client.FuturesUsdt.Order.CancelOrderAsync(symbol: Symbol.Name, orderId: orderId);
                if (res.Success)
                {
                    return res.Data;
                }
                else
                {
                    logger.LogError($"Error: {res.Error.Code} | Message: {res.Error.Message}");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to set limit order");
            }
            return null;
        }

        public async Task<BinanceFuturesPlacedOrder> SetLimit(decimal price, decimal volume, OrderSide direction)
        {
            try
            {
                var res = await client.FuturesUsdt.Order.PlaceOrderAsync(symbol: Symbol.Name,
                                                                   side: direction,
                                                                   type: OrderType.Limit,
                                                                   quantity: NormalizeLot(volume),
                                                                   price: NormalizePrice(price),
                                                                   timeInForce: TimeInForce.GoodTillCancel);
                if (res.Success)
                {
                    return res.Data;
                }
                else
                {
                    logger.LogError($"Error: {res.Error.Code} | Message: {res.Error.Message}");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to set limit order");
            }
            return null;
        }

        public async Task<BinanceFuturesPlacedOrder> SetMarket(decimal volume, OrderSide direction)
        {
            try
            {
                var res = await client.FuturesUsdt.Order.PlaceOrderAsync(symbol: Symbol.Name,
                                                                         side: direction,
                                                                         type: OrderType.Market,
                                                                         quantity: NormalizeLot(volume));
                if (res.Success)
                {
                    return res.Data;
                }
                else
                {
                    logger.LogError($"Error: {res.Error.Code} | Message: {res.Error.Message}");
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while trying to set limit order");
            }
            return null;
        }
        public async Task CloseAllForBot(Bot stoppingBot)
        {
            stoppingBot.IsTradeEnabled = false;
            try
            {
                bool checkInitialOrder = true;
                if (stoppingBot.TpOrder != null)
                {
                    if (stoppingBot.TpOrder.Status == OrderStatus.New)
                    {
                        var cancel = await CancelOrder(stoppingBot.TpOrder.Id);
                        if (cancel == null)
                        {
                            logger.LogWarning($"{Symbol.Name} | Can`t cancl TP order. you have to cancel it manualy. order id = {stoppingBot.TpOrder.Id}");
                        }
                    }
                    else if (stoppingBot.TpOrder.Status == OrderStatus.PartiallyFilled)
                    {
                        var cancel = await CancelOrder(stoppingBot.TpOrder.Id);
                        if (cancel == null)
                        {
                            logger.LogWarning($"{Symbol.Name} | Can`t cancl TP order. you have to cancel it manualy. order id = {stoppingBot.TpOrder.Id}");
                        }
                        var closeRest = await SetMarket(stoppingBot.TpOrder.ActualVolume, stoppingBot.TpOrder.Direction);
                        if (closeRest == null)
                        {
                            logger.LogWarning($"{Symbol.Name} | Can`t close rest TP order`s volume. The position was partialy closed, other position part {stoppingBot.TpOrder.ActualVolume} contracts will be ignored");
                        }
                        else
                            checkInitialOrder = false;
                    }
                    else if (stoppingBot.TpOrder.Status == OrderStatus.Filled)
                    {
                        checkInitialOrder = false;
                    }
                }
                if (checkInitialOrder && stoppingBot.InitialOrder != null)
                {
                    if (stoppingBot.InitialOrder.Status == OrderStatus.Filled)
                    {
                        var closeRest = await SetMarket(stoppingBot.InitialOrder.FilledVolume, stoppingBot.InitialOrder.Direction == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy);
                        if (closeRest == null)
                        {
                            logger.LogWarning($"{Symbol.Name} | Can`t close initial order`s volume. The position was partialy opened, position`s part {stoppingBot.InitialOrder.FilledVolume} contracts will be ignored");
                        }
                    }
                    else if (stoppingBot.InitialOrder.Status == OrderStatus.PartiallyFilled)
                    {
                        var cancel = await CancelOrder(stoppingBot.InitialOrder.Id);
                        if (cancel == null)
                        {
                            logger.LogWarning($"{Symbol.Name} | Can`t cancl initial order. you have to cancel it manualy. order id = {stoppingBot.TpOrder.Id}");
                        }
                        var closeRest = await SetMarket(stoppingBot.InitialOrder.FilledVolume, stoppingBot.InitialOrder.Direction == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy);
                        if (closeRest == null)
                        {
                            logger.LogWarning($"{Symbol.Name} | Can`t close initial order`s volume. The position was partialy opened, position`s part {stoppingBot.InitialOrder.FilledVolume} contracts will be ignored");
                        }
                    }
                    else if (stoppingBot.InitialOrder.Status == OrderStatus.New)
                    {
                        var cancel = await CancelOrder(stoppingBot.InitialOrder.Id);
                        if (cancel == null)
                        {
                            logger.LogWarning($"{Symbol.Name} | Can`t cancl initial order. you have to cancel it manualy. order id = {stoppingBot.TpOrder.Id}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Can`t close active positions for the bot it will be ignored.");
            }
            finally
            {
                stoppingBot.TpOrder = null;
                stoppingBot.InitialOrder = null;
                stoppingBot.SlPrice = 0;
            }

        }
    }
}
