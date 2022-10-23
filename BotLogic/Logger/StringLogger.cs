using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BotLogic.Logger
{
    public class StringLogger : ILogger
    {
        public StringLogger(Func<string> pathGetter)
        {
            this.pathGetter = pathGetter;
        }

        readonly Func<string> pathGetter;

        public async Task LogError(Exception e, string message) => await Log($"ERROR | {message}: {e}");
        public async Task LogError(string message) => await Log($"ERROR | {message}");
        public async Task LogInformation(string message) => await Log($"INFORMATION | {message}");
        public async Task LogWarning(string message) => await Log($"WARNING | {message}");


        private readonly SemaphoreSlim semaphore = new(1);
        private async Task Log(string message)
        {
            await semaphore.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(pathGetter(), $"{DateTime.Now} | {message}\n", System.Text.Encoding.UTF8);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
