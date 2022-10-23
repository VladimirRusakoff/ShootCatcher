using ShootCatcher.Helpers;
using ShootCatcher.View;
using System;
using System.Windows;

namespace ShootCatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            AddStratagyWindowHelper.Close();
            BotPositionsViewerHelper.CloseAll();
            if (DataContext is MainView viewModel)
            {
                viewModel.Disconnect.Execute(null);
            }
        }
    }
}
