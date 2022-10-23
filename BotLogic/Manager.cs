using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Futures.FuturesData;
using BotLogic.Logger;
using BotLogic.Logic;
using BotLogic.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotLogic
{
    public class Manager : IAsyncDisposable
    {
        public Manager(LoggerCollection logger, string key, string secret)
        {
            this.logger = logger;

            client = new(new() { ApiCredentials = new(key, secret) });
            clintForTrader = new(new() { ApiCredentials = new(key, secret) });
            soketClient = new(new() { ApiCredentials = new(key, secret), AutoReconnect = true, ReconnectInterval = new(0, 0, 1) });
            if (!StartStream())
                throw new Exception("Can`t start stream");
        }

        private readonly LoggerCollection logger;

        private readonly BinanceClient client, clintForTrader;
        private readonly BinanceSocketClient soketClient;
        private readonly List<Trader> traders = new();
        private readonly List<IBot> bots = new();

        private const string USDT_NAME = "USDT";

        public event Action<TradeDetales> OnTradeTransaction;
        public event Action<List<PositionInfo>> OnPositionUpdate;

        private bool StartStream()
        {
            var listenKeyResult = client.FuturesUsdt.UserStream.StartUserStream();
            if (!listenKeyResult.Success)
                return false;
            var successAccount = soketClient.FuturesUsdt.SubscribeToUserDataUpdates(listenKeyResult.Data, null, null,
                accountUpdate =>
                {
                    List<PositionInfo> positions = new();
                    foreach (var position in accountUpdate.UpdateData.Positions)
                    {
                        if (position.Symbol.EndsWith(USDT_NAME) && position.PositionAmount != 0)
                        {
                            positions.Add(new()
                            {
                                Symbol = position.Symbol,
                                Amount = position.PositionAmount,
                                EntryPrice = position.EntryPrice,
                                RealizedPnL = position.RealizedPnL,
                                Side = position.PositionSide,
                                UnrealizedPnl = position.UnrealizedPnl
                            });
                        }
                    }

                    OnPositionUpdate?.Invoke(positions);
                },
                orderUpdate =>
                {
                    TradeDetales tradeDetales = new()
                    {
                        Symbol = orderUpdate.UpdateData.Symbol,
                        Id = orderUpdate.UpdateData.OrderId,
                        Direction = orderUpdate.UpdateData.Side,
                        Status = orderUpdate.UpdateData.Status,
                        Price = orderUpdate.UpdateData.Price,
                        ActualVolume = Math.Abs(orderUpdate.UpdateData.Quantity),
                        FilledVolume = Math.Abs(orderUpdate.UpdateData.AccumulatedQuantityOfFilledTrades)
                    };

                    OnTradeTransaction?.Invoke(tradeDetales);
                },
                streamEvent =>
                {
                    //StopStream().GetAwaiter().GetResult();
                    logger.LogInformation("Web socket kry expired.");
                    if (StartStream())
                        logger.LogInformation("Reconnected to the websocket");
                    else
                        logger.LogWarning("Can`t reconnect to the websocket. Bots will bot be updated anymore! You have to restart program!!!");
                });
            return successAccount.Success;
        }

        public IBot AddBot(Binance.Net.Enums.OrderSide side, decimal volume, decimal buffer,
                           decimal distance, decimal sl, decimal tp, string asset)
        {
            if (disposed)
                return null;

            var trader = traders.FirstOrDefault(x => x.Symbol.Name == asset);
            if (trader == null)
            {
                trader = new(clintForTrader, soketClient, logger, asset, this);
                traders.Add(trader);
            }
            var bot = new Bot(trader, logger)
            {
                Direction = side,
                Distance = distance / 100, // distance > 1 ? distance / 100 : distance, //VR+
                Volume = volume,
                Buffer = buffer / 200, //buffer > 1 ? buffer / 100 : buffer,
                SL = sl / 100, //sl > 1 ? sl / 100 : sl,
                TP = tp / 100 //tp > 1 ? tp / 100 : tp
            };

            if (!bots.Any(x => x.IsEqual(bot)))
            {
                bots.Add(bot);
                return bot;
            }
            return null;
        }

        public async Task StopBot(IBot bot)
        {
            if (disposed)
                return;
            if (bot is Bot concreteBot && concreteBot.Trader is Trader trader)
            {
                concreteBot.IsTradeEnabled = false;
                await trader.CloseAllForBot(concreteBot);
            }
        }
        public void StartBot(IBot bot)
        {
            if (disposed)
                return;
            if (bot is Bot concreteBot && bots.Any(x => x.IsEqual(bot)))
            {
                concreteBot.InitialOrder = null;
                concreteBot.SlPrice = 0;
                concreteBot.TpOrder = null;
                concreteBot.IsTradeEnabled = true;
            }
        }
        public async Task<bool> RemoveBot(IBot bot)
        {
            if (disposed)
                return false;
            await StopBot(bot);
            bots.Remove(bot);
            return true;
        }

        private async Task StopStream()
        {
            await soketClient.UnsubscribeAll();
        }

        public bool IsConnected
        {
            get
            {
                if (disposed)
                    return false;
                var accountStatus = client.General.GetAccountStatus();
                return accountStatus.Success && accountStatus.Data.Success;
            }
        }
        public BinanceFuturesAccountInfo AccountInfo
        {
            get
            {
                if (disposed)
                    return null;
                var accountInfo = client.FuturesUsdt.Account.GetAccountInfo();
                return (accountInfo.Success ? accountInfo.Data : null);
            }
        }
        public IEnumerable<BinanceFuturesOrder> Ordrs(string symbol)
        {
            if (disposed)
                return null;
            var orders = client.FuturesUsdt.Order.GetOpenOrders(symbol);
            return (orders.Success ? orders.Data : null);
        }

        private async Task CloseAll()
        {
            foreach (var trader in traders)
            {
                var _bots = bots.Where(x => x.Asset == trader.Symbol.Name);
                foreach (var bot in _bots)
                {
                    if (bot is Bot concretebot)
                        concretebot.IsTradeEnabled = false;
                }
                bots.RemoveAll(x => x.Asset == trader.Symbol.Name);

                await trader.CloseAll();
            }
        }

        public string[] Futures
        {
            get
            {
                List<string> ans = new();
                if (!disposed)
                {
                    var futuresUsdt = client.FuturesUsdt.System.GetExchangeInfo();
                    if (futuresUsdt.Success && futuresUsdt.Data != null && futuresUsdt.Data.Symbols.Any())
                    {
                        ans.AddRange(futuresUsdt.Data.Symbols.Where(x => x.ContractType == Binance.Net.Enums.ContractType.Perpetual).Select(x => x.Name).OrderBy(x => x));
                    }
                }
                return ans.ToArray();
            }
        }

        private bool disposed = false;

        public async ValueTask DisposeAsync()
        {
            disposed = true;
            try
            {
                await CloseAll();

                if (client != null)
                {
                    client.Dispose();
                }
                if (clintForTrader != null)
                {
                    clintForTrader.Dispose();
                }
                if (soketClient != null)
                {
                    await StopStream();
                    soketClient.Dispose();
                }
            }
            finally
            {
                logger.LogWarning("Bot manager disposed.");
            }
        }
    }

    public class PositionInfo
    {
        public string Symbol { get; set; }
        public PositionSide Side { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public decimal RealizedPnL { get; set; }
    }
}
