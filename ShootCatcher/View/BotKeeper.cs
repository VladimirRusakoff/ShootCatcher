using BotLogic.Logic;
using ShootCatcher.Helpers;
using ShootCatcher.Model;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace ShootCatcher.View
{
    class BotKeeper : INotifyPropertyChanged
    {
        public BotKeeper(IBot bot, Action<BotKeeper> removeFromCollection, BotLogicManager manager)
        {
            Bot = bot;


            Start = new RelayCommand(o =>
            {
                manager.StartBot(bot);
                OnPropertyChanged("StartBtnVisability");
                OnPropertyChanged("StopBtnVisability");
            });
            Stop = new RelayCommand(async o =>
            {
                await manager.StopBot(bot);
                OnPropertyChanged("StartBtnVisability");
                OnPropertyChanged("StopBtnVisability");
            });
            Delete = new RelayCommand(async o =>
            {
                if (await manager.RemoveBot(bot))
                {
                    removeFromCollection?.Invoke(this);
                    BotPositionsViewerHelper.Close(bot);
                }
            });
            ViewPositions = new RelayCommand(o =>
            {
                BotPositionsViewerHelper.Open(bot);
            });
        }

        public IBot Bot { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new(name));
        }

        public bool StartBtnVisability => !Bot.IsTradeEnabled;
        public bool StopBtnVisability => Bot.IsTradeEnabled;

        public ICommand Start { get; }
        public ICommand Stop { get; }
        public ICommand Delete { get; }
        public ICommand ViewPositions { get; }
    }
}
