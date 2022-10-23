using Binance.Net.Enums;
using BotLogic.Logic;
using BotLogic.Wrappers;
using ShootCatcher.Helpers;
using System;
using System.Collections.Generic;

namespace ShootCatcher.Model
{
    class BotSettings : Serializer<BotSettings>
    {
        public string APIkey { get; set; }
        public string Seckey { get; set; }
        public List<Settings> Settings { get; set; } = new();
    }

    class Settings
    {
        public OrderSide Direction { get; set; }
        public decimal Volume { get; set; }
        public decimal Buffer { get; set; }
        public decimal Distance { get; set; }
        public decimal SL { get; set; }
        public decimal TP { get; set; }
        public string Asset { get; set; }

        public bool Compare(Settings other)
        {
            return Direction == other.Direction &&
                   Volume == other.Volume &&
                   Buffer == other.Buffer &&
                   Distance == other.Distance &&
                   SL == other.SL &&
                   TP == other.TP &&
                   Asset == other.Asset;
        }
    }
}
