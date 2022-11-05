using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using QSB.Messaging;
using QSB.Player;
using Discord;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using QSB.Player.TransformSync;
using UnityEngine;
using Mirror;
using QSB;

namespace DiscordProximityChat{
    public class DiscordManager{
        private static DiscordManager _instance;

        public static DiscordManager instance{
            get{
                if (_instance == null)
                    _instance = new DiscordManager();
                return _instance;
            }
        }

        public readonly Dictionary<uint, long> PlayerDiscordID = new Dictionary<uint, long>();
        private readonly long clientID = 1032464113953165352;
        private string lobbySecret; //Only the host uses this variable

        public Discord.Discord discord;

        public DiscordManager(){
            _instance = this;

            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Initializing Discord API");
            //discord = new Discord.Discord(clientID, (long) Discord.CreateFlags.NoRequireDiscord);
            discord = new Discord.Discord(clientID, (UInt64) Discord.CreateFlags.Default);

            discord.SetLogHook(Discord.LogLevel.Debug, LogProblems);
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord API Initialized", MessageType.Success);
            discord.RunCallbacks(); //Run the callbacks at least once to try and get the UserID

            DiscordProximityChat.instance.ModHelper.Console.WriteLine("UserID :: " + getUserID(), MessageType.Success);

            QSBPlayerManager.OnAddPlayer += OnQSBAddPlayer;
            QSBPlayerManager.OnRemovePlayer += RemovePlayer;
            
            
        }

        public void RunCallbacks(){
            try{
                discord.RunCallbacks();
            }
            catch (Discord.ResultException e){
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord::" + e.Result, MessageType.Error);
            }
        }

        void LogProblems(Discord.LogLevel level, string message){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord:" + level + " - " + message,MessageType.Error);
        }

        long getUserID(){
            long id = 0;
            try{
                id = discord.GetUserManager().GetCurrentUser().Id;
            } catch (ResultException e){
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord::" + e.Result, MessageType.Error);
                discord.RunCallbacks();
                return getUserID();
            }
            return id;
        }

        void OnQSBAddPlayer(PlayerInfo info){
            DiscordProximityChat.instance.ModHelper.Events.Unity.RunWhen(() => PlayerTransformSync.LocalInstance, () => {
                SendLobbyMessage(info);
                if (info.IsLocalPlayer)
                        return;
                
                long discordUserID = getUserID();
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Sending QSB Message to " + info + " :: " + discordUserID + ")", MessageType.Info);
                new DiscordIDMessage(QSBPlayerManager.LocalPlayerId, discord.GetUserManager().GetCurrentUser().Id){To = info.PlayerId}.Send();
            });
        }

        void CreateDiscordLobby(){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Creating discord Lobby", MessageType.Info);
            //If we are the host create a discord lobby
            var txn = discord.GetLobbyManager().GetLobbyCreateTransaction();
            txn.SetCapacity((uint)QSBNetworkManager.singleton.maxConnections);
            txn.SetType(LobbyType.Public);

            discord.GetLobbyManager().CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobby) => {
                try{
                    if (result == Discord.Result.Ok){
                        lobbySecret = discord.GetLobbyManager().GetLobbyActivitySecret(lobby.Id);
                        discord.GetLobbyManager().ConnectNetwork(lobby.Id);
                        discord.GetLobbyManager().OpenNetworkChannel(lobby.Id, 0, true);
                        discord.GetLobbyManager().OpenNetworkChannel(lobby.Id, 1, false);
                        DiscordProximityChat.instance.ModHelper.Console.WriteLine("Created lobby " + lobby.Id, MessageType.Success);
                        
                        DiscordManager.instance.discord.GetLobbyManager().ConnectVoice(lobby.Id, (result) =>
                        {
                            if (result == Discord.Result.Ok)
                            {
                                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Voice connected!");
                            }
                        });
                    }
                }catch(ResultException e){
                    DiscordProximityChat.instance.ModHelper.Console.WriteLine("Failed to create Lobby! Trying Again", MessageType.Error);
                    CreateDiscordLobby();
                }
            });
        }

        void SendLobbyMessage(PlayerInfo info){
            if (QSBCore.IsHost){
                if(lobbySecret == null)
                    CreateDiscordLobby();
                discord.RunCallbacks(); //Cause Why not
            }

            DiscordProximityChat.instance.ModHelper.Events.Unity.RunWhen(() => lobbySecret != null, () => {
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Sending Lobby Secret to " + info + " :: " + lobbySecret + ")", MessageType.Info);
                new DiscordLobbyMessage(lobbySecret){To = info.PlayerId}.Send();
            });
        }

        void RemovePlayer(PlayerInfo info){
            PlayerDiscordID.Remove(info.PlayerId);
        }

        public void DiscordVolumeUpdater(){
            foreach (var playerKV in PlayerDiscordID){
                PlayerInfo player = QSBPlayerManager.GetPlayer(playerKV.Key);
                if (player == QSBPlayerManager.LocalPlayer)
                    return;
                float dist = (player.Body.transform.position - QSBPlayerManager.LocalPlayer.Body.transform.position).sqrMagnitude;
                float maxDist = 50*50;
                byte bolume = (byte) Mathf.Clamp(100 * (maxDist - dist) / maxDist, 0, 200);

                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Setting " + QSBPlayerManager.GetPlayer(playerKV.Key) + " Volume to " + bolume, MessageType.Info);
                discord.GetVoiceManager().SetLocalVolume(playerKV.Value, bolume);
            }
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
            if (DiscordManager.instance.PlayerDiscordID.ContainsKey(networkId))
                return;
            DiscordManager.instance.PlayerDiscordID.Add(networkId, discordID);
        }
    }

    public class DiscordLobbyMessage : QSBMessage<string>{

        public DiscordLobbyMessage(string data) : base(data){}
        
        public override void OnReceiveRemote(){
            ConnectToVoice();
        }
        
        public override void OnReceiveLocal(){
            ConnectToVoice();
        }

        public void ConnectToVoice(){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Receieved Lobby ID :: " + Data,MessageType.Success);
            DiscordManager.instance.discord.GetLobbyManager().ConnectLobbyWithActivitySecret(Data,(Result result, ref Lobby lobby) => {
                if (result == Discord.Result.Ok)
                {
                    DiscordProximityChat.instance.ModHelper.Console.WriteLine("Connected to lobby " + lobby.Id);
                    DiscordManager.instance.discord.GetLobbyManager().ConnectVoice(lobby.Id, (result) =>
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
}