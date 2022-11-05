using System;
using System.Collections.Generic;
using QSB.Messaging;
using QSB.Player;
using Discord;
using OWML.Common;
using QSB.Player.TransformSync;
using UnityEngine;
using QSB;

namespace DiscordProximityChat{
    public static class DiscordManager{

        public static readonly Dictionary<uint, long> PlayerDiscordID = new();
        public static readonly Dictionary<long, bool> isSpeaking = new();

        public static long lobbyID;
        private static string lobbySecret; //Only the host uses this variable
        public static Discord.Discord discord;
        
        public static void Init(){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Initializing Discord API");
            //discord = new Discord.Discord(clientID, (long) Discord.CreateFlags.NoRequireDiscord);
            discord = new Discord.Discord(Constants.clientID, (UInt64) Discord.CreateFlags.Default);

            discord.SetLogHook(Discord.LogLevel.Debug, LogProblems);
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord API Initialized", MessageType.Success);
            discord.RunCallbacks(); //Run the callbacks at least once to try and get the UserID

            DiscordProximityChat.instance.ModHelper.Console.WriteLine("UserID :: " + GetUserID(), MessageType.Success);

            QSBPlayerManager.OnAddPlayer += OnQSBAddPlayer;
            QSBPlayerManager.OnRemovePlayer += RemovePlayer;

            discord.GetLobbyManager().OnSpeaking += OnSpeaking;

        }

        public static void RunCallbacks(){
            try{
                discord.RunCallbacks();
            }
            catch (Discord.ResultException e){
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord::" + e.Result, MessageType.Error);
            }
        }

        static void OnSpeaking(long lobbyId, long userId, bool speaking){
            isSpeaking[userId] = speaking;
        }

        static void LogProblems(Discord.LogLevel level, string message){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord:" + level + " - " + message,MessageType.Error);
        }

        static long GetUserID(){
            long id = 0;
            try{
                id = discord.GetUserManager().GetCurrentUser().Id;
            } catch (ResultException e){
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord::" + e.Result, MessageType.Error);
                discord.RunCallbacks();
                return GetUserID();
            }
            return id;
        }

        static void OnQSBAddPlayer(PlayerInfo info){
            DiscordProximityChat.instance.ModHelper.Events.Unity.RunWhen(() => PlayerTransformSync.LocalInstance, () => {
                SendLobbyMessage(info);
                if (info.IsLocalPlayer)
                        return;
                
                long discordUserID = GetUserID();
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Sending QSB Message to " + info + " :: " + discordUserID + ")", MessageType.Info);
                new DiscordIDMessage(QSBPlayerManager.LocalPlayerId, discord.GetUserManager().GetCurrentUser().Id){To = info.PlayerId}.Send();
            });
        }

        static void CreateDiscordLobby(){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine("Creating discord Lobby", MessageType.Info);
            //If we are the host create a discord lobby
            var txn = discord.GetLobbyManager().GetLobbyCreateTransaction();
            txn.SetCapacity((uint)QSBNetworkManager.singleton.maxConnections);
            txn.SetType(LobbyType.Public);

            discord.GetLobbyManager().CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobby) => {
                try{
                    if (result == Discord.Result.Ok){
                        lobbySecret = discord.GetLobbyManager().GetLobbyActivitySecret(lobby.Id);
                        DiscordProximityChat.instance.ModHelper.Console.WriteLine("Created lobby " + lobby.Id, MessageType.Success);
                        
                        DiscordManager.discord.GetLobbyManager().ConnectVoice(lobby.Id, (result) =>
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

        static void SendLobbyMessage(PlayerInfo info){
            if (QSBCore.IsHost){
                if(lobbySecret == null)
                    CreateDiscordLobby();
                discord.RunCallbacks(); //Cause Why not
                
                DiscordProximityChat.instance.ModHelper.Events.Unity.RunWhen(() => lobbySecret != null, () => {
                    DiscordProximityChat.instance.ModHelper.Console.WriteLine("Sending Lobby Secret to " + info + " :: " + lobbySecret + ")", MessageType.Info);
                    new DiscordLobbyMessage(lobbySecret){To = info.PlayerId}.Send();
                });
            }
        }

        static void RemovePlayer(PlayerInfo info){
            PlayerDiscordID.Remove(info.PlayerId);

            if (lobbyID == 0)
                return;

            if (info.IsLocalPlayer){
                discord.GetLobbyManager().DisconnectVoice(lobbyID, result => {
                    discord.GetLobbyManager().DisconnectLobby(lobbyID, result1 => {
                        //Hope for the best
                    });
                });
            }

        }
        public static void DiscordVolumeUpdater(){
            foreach (var playerKV in PlayerDiscordID){
                if (!isSpeaking[playerKV.Value]) //If we aren't speaking don't worry about setting volume
                    return;
                
                PlayerInfo player = QSBPlayerManager.GetPlayer(playerKV.Key);
                if (!player.IsReady)
                    return;
                
                if (player == QSBPlayerManager.LocalPlayer)
                    return;
                
                float dist = (player.Body.transform.position - QSBPlayerManager.LocalPlayer.Body.transform.position).magnitude;
                float maxDist = 50;
                int bolume = (byte) Mathf.Clamp(150 * (maxDist - dist) / maxDist, 0, 200);

                if(QSBPlayerManager.LocalPlayer.SignalscopeEquipped){
                    bolume = Mathf.Max(bolume,(int) Mathf.Clamp((150 * QSBPlayerManager.LocalPlayer.LocalSignalscope._strongestSignals[0]._activeVolume), 0,150));
                }
                
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Setting " + QSBPlayerManager.GetPlayer(playerKV.Key) + " Volume to " + discord.GetVoiceManager().GetLocalVolume(playerKV.Value), MessageType.Info);
                discord.GetVoiceManager().SetLocalVolume(playerKV.Value, (byte)bolume);
            }
        }
    }
}
