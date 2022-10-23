using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotLogic.Logger
{
    public class LoggerCollection
    {
        public LoggerCollection(IEnumerable<ILogger> loggers)
        {
            this.loggers = loggers;
        }

        readonly IEnumerable<ILogger> loggers;

        public async void LogError(Exception e, string message) => await Log(logger => logger.LogError(e, message));
        public async void LogError(string message) => await Log(logger => logger.LogError(message));
        public async void LogInformation(string message) => await Log(logger => logger.LogInformation(message));
        public async void LogWarning(string message) => await Log(logger => logger.LogWarning(message));

        private async Task Log(Func<ILogger, Task> log)
        {
            foreach (var logger in loggers)
                await log.Invoke(logger);
        }
    }
}
