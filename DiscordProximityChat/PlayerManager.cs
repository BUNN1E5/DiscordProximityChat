using System.Collections.Generic;
using QSB.Player;

namespace DiscordProximityChat{
    public class PlayerManager{
        public List<PlayerInfo> Hiders;
        public List<PlayerInfo> Seekers;


        public PlayerManager(){
            Hiders = new List<PlayerInfo>();
            Seekers = new List<PlayerInfo>();
        }

    }
}