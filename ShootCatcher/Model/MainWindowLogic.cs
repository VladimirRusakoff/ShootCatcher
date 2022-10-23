using Binance.Net.Objects.Futures.FuturesData;
using System;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using BotLogic.Logger;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Timer = System.Timers.Timer;

namespace ShootCatcher.Model
{
    class MainWindowLogic
    {

        public MainWindowLogic(Dispatcher dispatcher)
        {
            Logger = new(new List<ILogger>
            {
                new StringLogger(() =>
                {
                    var logDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"Logs");
                    if(!Directory.Exists(logDir))
                        Directory.CreateDirectory(logDir);
                    return Path.Combine(logDir, $"{DateTime.Now:ddMMyyyy}.log");
                }),
                new GUILogger(LogsKeeper, dispatcher)
            });
            Logic = BotLogicManager.Instance(Logger);
        }

        private Timer timer;

        private void Logic_AccountInfoChanged()
        {
            AccountData = Logic.AccountInfo;
            AccountDataUpdate?.Invoke();
        }

        public LoggerCollection Logger { get; }
        public BotLogicManager Logic { get; }
        public BinanceFuturesAccountInfo AccountData { get; private set; } = new();
        public ObservableCollection<LogItem> LogsKeeper { get; } = new();

        public async Task Subscribe(string key, string secret)
        {
            await Logic.Connect(key, secret);
            Logic_AccountInfoChanged();
            if (timer == null)
            {
                timer = new Timer(5000);
                // Hook up the Elapsed event for the timer. 
                timer.Elapsed += (object sender, ElapsedEventArgs e) => Logic_AccountInfoChanged();
                timer.AutoReset = true;
                timer.Enabled = true;
            }
            else
                timer.Start();
        }
        public async Task Unsubscribe()
        {
            timer.Stop();
            timer.Close();
            await Logic.Disconnect();
        }

        public event Action AccountDataUpdate;

    }
}
