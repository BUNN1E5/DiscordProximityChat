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

        public static readonly BidirectionalDictionary<uint, long> PlayerDiscordID = new();
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
            } catch (Discord.ResultException e){
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
            } catch (ResultException e) {
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Discord::" + e.Result, MessageType.Error);
                discord.RunCallbacks();
                return GetUserID();
            }
            return id;
        }

        static void OnQSBAddPlayer(PlayerInfo info){
            DiscordProximityChat.instance.ModHelper.Events.Unity.RunWhen(() => PlayerTransformSync.LocalInstance, () => {
                SendLobbyMessage(info);
                var discordUserID = GetUserID();
                if (info.IsLocalPlayer){
                    PlayerDiscordID[QSBPlayerManager.LocalPlayerId] = discordUserID;
                    return;
                }
                
                DiscordProximityChat.instance.ModHelper.Console.WriteLine("Sending QSB Message to " + info + " :: " + discordUserID + ")", MessageType.Info);
                new DiscordIDMessage(QSBPlayerManager.LocalPlayerId, discordUserID){To = info.PlayerId}.Send();
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
                        
                        DiscordManager.discord.GetLobbyManager().ConnectVoice(lobby.Id, (result) => {
                            if (result == Discord.Result.Ok){
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
            foreach (KeyValuePair<uint, long> playerKV in PlayerDiscordID){ //Explicit cause I'm bad at programming
                if (!QSBPlayerManager.PlayerExists(playerKV.Key))
                    continue;

                PlayerInfo player = QSBPlayerManager.GetPlayer(playerKV.Key);

                if (!player.IsReady)
                    continue;

                if (player.IsLocalPlayer)
                    continue;

                if (player.Body == null || QSBPlayerManager.LocalPlayer.Body == null)
                    continue;

                if (isSpeaking.TryGetValue(playerKV.Value, out bool speaking) && !speaking) //This works cause the TryGetValue gets evaluated first
                    continue;

                float dist = (player.Body.transform.position - QSBPlayerManager.LocalPlayer.Body.transform.position).magnitude;
                float maxVol = DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<float>("Global Volume");
                
                float bolume = CalcBolume(dist);

                if (DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<bool>("Scout Speaker")){
                    //Probe acts as speaker
                    if (player.ProbeBody != null){
                        float probeDist = (player.ProbeBody.transform.position - QSBPlayerManager.LocalPlayer.Body.transform.position).magnitude;
                        bolume = Mathf.Max(bolume,CalcBolume(probeDist));
                    }
                }
                
                if (QSBPlayerManager.LocalPlayer == null){
                    DiscordProximityChat.instance.ModHelper.Console.WriteLine("LocalPlayer is null, How did we get here?", MessageType.Error);
                    continue;
                }

                #region SignalScope

                if(QSBPlayerManager.LocalPlayer.SignalscopeEquipped){
                    if (QSBPlayerManager.LocalPlayer.LocalSignalscope._strongestSignals == null)
                        continue;

                    if (Constants.PlayerSignals == null){
                        DiscordProximityChat.instance.ModHelper.Console.WriteLine("PlayerSignals is null, How did we get here?", MessageType.Error);
                        continue;
                    }

                    if (Constants.PlayerSignals.TryGetValue(player, out AudioSignal signal)){
                        bolume = Mathf.Max(bolume,Mathf.Clamp(maxVol *  signal._signalStrength, 0, maxVol));
                    }
                }
                
                #endregion
                
                //DiscordProximityChat.instance.ModHelper.Console.WriteLine("bolume for " + player.Name + " : " + bolume + " | " + discord.GetVoiceManager().GetLocalVolume(playerKV.Key), MessageType.Info);
                
                discord.GetVoiceManager().SetLocalVolume(playerKV.Value, (byte)bolume);
            }
        }

        private static float CalcBolume(float dist){
            float a = DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<float>("Audio Falloff Offset (A)");
            float b = DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<float>("Audio Falloff Power (B)");
            float maxVol = DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<float>("Global Volume");
            float maxDist = DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<float>("Max Audio Distance");
            bool clip = DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<bool>("Mute at Max Distance");
            
            return Mathf.Clamp(maxVol * Utils.CalculateProximityVolume(1 - ((maxDist - dist) / maxDist), a, b, clip), 0, maxVol);
        }
    }
}
