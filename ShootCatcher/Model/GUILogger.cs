using BotLogic.Logger;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace ShootCatcher.Model
{
    class GUILogger : ILogger
    {
        public GUILogger(ObservableCollection<LogItem> logskeeper, Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            this.logskeeper = logskeeper;
        }

        private readonly ObservableCollection<LogItem> logskeeper;
        private readonly Dispatcher dispatcher;
        private readonly SemaphoreSlim semaphore = new(1);


        public async Task LogError(Exception e, string message) => await LogAsync($"{message}: {e} ", LogType.Error);
        public async Task LogError(string message) => await LogAsync(message, LogType.Error);
        public async Task LogInformation(string message) => await LogAsync(message, LogType.Information);
        public async Task LogWarning(string message) => await LogAsync(message, LogType.Warning);

        private async Task LogAsync(string message, LogType type)
        {
            await semaphore.WaitAsync();
            try
            {
                await dispatcher.BeginInvoke(() =>
                {
                    logskeeper.Add(new()
                    {
                        Type = type,
                        Message = message,
                        Time = DateTime.Now
                    });
                });
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    class LogItem
    {
        public DateTime Time { get; set; }
        public LogType Type { get; set; }
        public string Message { get; set; }
        public Brush Color
        {
            get
            {
                return Type switch
                {
                    LogType.Error => Brushes.DarkRed,
                    LogType.Warning => Brushes.Orange,
                    LogType.Information => Brushes.White,
                    _ => Brushes.White,
                };
            }
        }
    }

    enum LogType
    {
        Error,
        Warning,
        Information
    }
}
