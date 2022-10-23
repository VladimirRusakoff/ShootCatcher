using Binance.Net.Enums;
using Binance.Net.Objects.Futures.FuturesData;
using BotLogic;
using BotLogic.Logger;
using BotLogic.Logic;
using BotLogic.Wrappers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShootCatcher.Model
{
    class BotLogicManager
    {
        #region Instance
        private static BotLogicManager instance;
        public static BotLogicManager Instance(LoggerCollection logger)
        {
            if (instance == null)
                instance = new(logger);
            return instance;
        }
        private BotLogicManager(LoggerCollection logger)
        {
            this.logger = logger;
        }
        #endregion;

        private Manager manager;
        private readonly LoggerCollection logger;

        public event Action<TradeDetales> OnTradeTransaction;
        public event Action<List<PositionInfo>> OnPositionUpdate;
        public event Action<List<IBot>> AddedNewBots;

        public bool ConnectionStatus => manager != null && manager.IsConnected;

        public async Task Connect(string key, string secret)
        {
            try
            {
                manager = new(logger, key, secret);
                manager.OnTradeTransaction += Manager_OnTradeTransaction;
                manager.OnPositionUpdate += Manager_OnPositionUpdate;
                logger.LogInformation("Connected sucsessfuly");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Can`t connect to the Binance");
                await Disconnect();
            }
        }

        private void Manager_OnPositionUpdate(List<PositionInfo> obj) => OnPositionUpdate?.Invoke(obj);

        private void Manager_OnTradeTransaction(TradeDetales obj) => OnTradeTransaction?.Invoke(obj);

        public async Task Disconnect()
        {
            try
            {
                if (manager != null)
                {
                    await manager.DisposeAsync();
                    manager.OnTradeTransaction -= Manager_OnTradeTransaction;
                    manager.OnPositionUpdate -= Manager_OnPositionUpdate;
                    manager = null;
                }
                logger.LogInformation("Disconnected");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while disconnection from account !");
            }
        }

        public BinanceFuturesAccountInfo AccountInfo => manager?.AccountInfo ?? (new());

        public string[] Futures => manager != null ? manager.Futures : Array.Empty<string>();
        public void AddNewBot(BotSettingsLogic settings)
        {
            List<IBot> ans = new();
            if (manager != null)
            {
                foreach (var asset in settings.SelectedAssets)
                {
                    var bot = manager.AddBot(settings.Direction, settings.Volume, settings.Buffer, settings.Distance, settings.SL, settings.TP, asset.Asset);
                    if (bot != null)
                        ans.Add(bot);
                }
            }
            if (ans.Count > 0)
            {
                AddedNewBots?.Invoke(ans);
            }
        }

        public void AddNewBot(string asset, OrderSide direction, decimal volume, decimal buffer, decimal distance, decimal sl, decimal tp)
        {
            if (manager != null)
            {
                var bot = manager.AddBot(direction, volume, buffer, distance, sl, tp, asset);
                if (bot != null)
                    AddedNewBots?.Invoke(new() { bot });
            }
        }

        public async Task StopBot(IBot bot)
        {
            if (manager == null)
                return;
            await manager.StopBot(bot);
        }
        public void StartBot(IBot bot)
        {
            if (manager == null)
                return;
            manager.StartBot(bot);
        }
        public async Task<bool> RemoveBot(IBot bot)
        {
            if (manager == null)
                return false;
            return await manager.RemoveBot(bot);
        }
    }
}
