using System.Collections.Generic;
using OWML.Utils;
using QSB.Player;

namespace DiscordProximityChat{
    public static class Constants{
        public static BidirectionalDictionary<PlayerInfo, SignalName> PlayerSignalNames = new();
        public static BidirectionalDictionary<PlayerInfo, AudioSignal> PlayerSignals = new();
        public static readonly long clientID = 1032464113953165352;
    }
}