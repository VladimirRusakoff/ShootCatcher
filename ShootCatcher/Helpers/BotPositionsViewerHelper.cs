using BotLogic.Logic;
using System.Collections.Generic;

namespace ShootCatcher.Helpers
{
    static class BotPositionsViewerHelper
    {
        private static readonly Dictionary<IBot, PositionsViewer> windows = new();

        public static void Open(IBot bot)
        {
            if (bot != null)
            {
                if (windows.ContainsKey(bot))
                {
                    windows[bot].Close();
                }
                PositionsViewer botViewer = new(bot);
                windows[bot] = botViewer;
                botViewer.Show();
            }
        }
        public static void CloseAll()
        {
            foreach (var viewer in windows)
            {
                viewer.Value.Close();
            }
        }
        public static void Close(IBot bot)
        {
            if (windows.ContainsKey(bot))
            {
                windows[bot].Close();
                windows.Remove(bot);
            }
        }

    }
}
