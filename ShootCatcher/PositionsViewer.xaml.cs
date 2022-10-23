using BotLogic.Logic;
using ShootCatcher.View;
using System.Windows;

namespace ShootCatcher
{
    /// <summary>
    /// Логика взаимодействия для PositionsViewer.xaml
    /// </summary>
    public partial class PositionsViewer : Window
    {
        public PositionsViewer(IBot bot)
        {
            InitializeComponent();
            if (DataContext is BotPositionView vm)
            {
                vm.SetBot(bot);
            }
            Closed += PositionsViewer_Closed;
        }

        private void PositionsViewer_Closed(object sender, System.EventArgs e)
        {
            if (DataContext is BotPositionView vm)
            {
                vm.Unsubscribe();
            }
        }
    }
}
