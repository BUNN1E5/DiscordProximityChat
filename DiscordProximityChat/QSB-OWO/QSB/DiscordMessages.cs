using Discord;
using Mirror;
using OWML.Common;
using QSB.Messaging;
using QSB.Player;

namespace DiscordProximityChat{
    public class DiscordLobbyMessage : QSBMessage<string>{

        public DiscordLobbyMessage(string data) : base(data){}
        
        public override void OnReceiveRemote(){
            ConnectToVoice();
        }
        
        public override void OnReceiveLocal(){
            ConnectToVoice();
        }

        public void ConnectToVoice(){
            Utils.WriteLine("Receieved Lobby ID :: " + Data, MessageType.Success);
            DiscordManager.discord.GetLobbyManager().ConnectLobbyWithActivitySecret(Data,(Result result, ref Lobby lobby) => {
                if (result == Discord.Result.Ok){
                    Utils.WriteLine("Connected to lobby " + lobby.Id);
                    DiscordManager.lobbyID = lobby.Id;
                    DiscordManager.discord.GetLobbyManager().ConnectVoice(lobby.Id, (result) =>
                    {
                        if (result == Discord.Result.Ok)
                        {
                            Utils.WriteLine("Voice connected!");
                        }
                    });
                }
            });
        }
    }
    public class DiscordIDMessage : QSBMessage{

        private uint networkId;
        private long discordID;

        public DiscordIDMessage(uint networkId, long discordID) : base(){
            this.networkId = networkId;
            this.discordID = discordID;
        }

        public override void Serialize(NetworkWriter writer){
            base.Serialize(writer);
            writer.Write(networkId);
            writer.Write(discordID);
        }

        public override void Deserialize(NetworkReader reader){
            base.Deserialize(reader);
            networkId = reader.Read<uint>();
            discordID = reader.Read<long>();
        }

        public override void OnReceiveRemote(){
            Utils.WriteLine("Recieved QSB Message From " + QSBPlayerManager.GetPlayer(networkId) + " :: " + discordID,MessageType.Success);
            if (DiscordManager.PlayerDiscordID.Contains(networkId))
                return;
            DiscordManager.PlayerDiscordID.Add(networkId, discordID);
        }
    }

    public class SharedSettingsMessage : QSBMessage{
        private bool bidirectionalSignalscope;
        private long discordID;
        
        public SharedSettingsMessage(IModConfig config, long discordID) : base(){
            this.bidirectionalSignalscope = config.GetSettingsValue<bool>("Bidirectional Signalscope");
            this.discordID = discordID;
        }
        
        public override void Serialize(NetworkWriter writer){
            base.Serialize(writer);
            writer.Write(bidirectionalSignalscope);
            writer.Write(discordID);
        }

        public override void Deserialize(NetworkReader reader){
            base.Deserialize(reader);
            bidirectionalSignalscope = reader.Read<bool>();
            discordID = reader.Read<long>();
        }
        
        public override void OnReceiveRemote(){
            DiscordManager.sharedSettings[discordID].BidirectionalSignalscope = bidirectionalSignalscope;
        }
    }

    public class DiscordIsTalkingMessage : QSBMessage{
        private bool isTalking;
        private long discordID;
        
        public DiscordIsTalkingMessage(bool isTalking, long discordID) : base(){
            this.isTalking = isTalking;
            this.discordID = discordID;
        }

        public override void Serialize(NetworkWriter writer){
            base.Serialize(writer);
            writer.Write(isTalking);
            writer.Write(discordID);
        }

        public override void Deserialize(NetworkReader reader){
            base.Deserialize(reader);
            isTalking = reader.Read<bool>();
            discordID = reader.Read<long>();
        }

        public override void OnReceiveRemote(){
            DiscordManager.isSpeaking[discordID] = isTalking;
        }
    }
}