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
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Receieved Lobby ID :: " + Data, MessageType.Success);
            DiscordManager.discord.GetLobbyManager().ConnectLobbyWithActivitySecret(Data,(Result result, ref Lobby lobby) => {
                if (result == Discord.Result.Ok){
                    DiscordProximityChat.instance.ModHelper.Console.WriteLine("Connected to lobby " + lobby.Id);
                    DiscordManager.lobbyID = lobby.Id;
                    DiscordManager.discord.GetLobbyManager().ConnectVoice(lobby.Id, (result) =>
                    {
                        if (result == Discord.Result.Ok)
                        {
                            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Voice connected!");
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
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Recieved QSB Message From " + QSBPlayerManager.GetPlayer(networkId) + " :: " + discordID,MessageType.Success);
            if (DiscordManager.PlayerDiscordID.ContainsKey(networkId))
                return;
            DiscordManager.PlayerDiscordID.Add(networkId, discordID);
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