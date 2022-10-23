using System;
using System.Threading.Tasks;

namespace BotLogic.Logger
{
    public interface ILogger
    {
        public Task LogError(Exception e, string message);
        public Task LogError(string message);
        public Task LogWarning(string message);
        public Task LogInformation(string message);
    }
}
