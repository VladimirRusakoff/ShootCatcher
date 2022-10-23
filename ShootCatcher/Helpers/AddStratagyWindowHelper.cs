using System.Windows;

namespace ShootCatcher.Helpers
{
    static class AddStratagyWindowHelper
    {
        static Window window;
        public static void Open()
        {
            if (window == null)
            {
                window = new StratagySettings();
                window.Show();

                window.Closed += Window_Closed;
            }
            else
                window.Activate();
        }
        public static void Close()
        {
            if (window != null)
                window.Close();
        }

        private static void Window_Closed(object sender, System.EventArgs e)
        {
            window = null;
        }
    }
}
