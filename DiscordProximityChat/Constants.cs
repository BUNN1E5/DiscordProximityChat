using System.Collections.Generic;
using OWML.Utils;
using QSB.Player;

namespace DiscordProximityChat{
    public static class Constants{
        public static Dictionary<PlayerInfo, SignalName> PlayerSignals = new();
        public static Dictionary<SignalName, PlayerInfo> ReversePlayerSignals = new(); //Jank

        public static readonly long clientID = 1032464113953165352;
    }
}