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
            Utils.WriteLine("Initializing Discord API");
            //discord = new Discord.Discord(clientID, (long) Discord.CreateFlags.NoRequireDiscord);
            discord = new Discord.Discord(Constants.clientID, (UInt64) Discord.CreateFlags.Default);

            discord.SetLogHook(Discord.LogLevel.Debug, LogProblems);
            Utils.WriteLine("Discord API Initialized", MessageType.Success);
            discord.RunCallbacks(); //Run the callbacks at least once to try and get the UserID

            Utils.WriteLine("UserID :: " + GetUserID(), MessageType.Success);

            QSBPlayerManager.OnAddPlayer += OnQSBAddPlayer;
            QSBPlayerManager.OnRemovePlayer += RemovePlayer;

            discord.GetLobbyManager().OnSpeaking += OnSpeaking;
        }

        public static void RunCallbacks(){
            try{
                discord.RunCallbacks();
            } catch (Discord.ResultException e){
                Utils.WriteLine("Discord::" + e.Result, MessageType.Error);
            }
        }

        static void OnSpeaking(long lobbyId, long userId, bool speaking){
            isSpeaking[userId] = speaking;
        }

        static void LogProblems(Discord.LogLevel level, string message){
            Utils.WriteLine("Discord:" + level + " - " + message,MessageType.Error);
        }

        static long GetUserID(){
            long id = 0;
            try{
                id = discord.GetUserManager().GetCurrentUser().Id;
            } catch (ResultException e) {
                Utils.WriteLine("Discord::" + e.Result, MessageType.Error);
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
                
                Utils.WriteLine("Sending QSB Message to " + info + " :: " + discordUserID + ")", MessageType.Info);
                new DiscordIDMessage(QSBPlayerManager.LocalPlayerId, discordUserID){To = info.PlayerId}.Send();
            });
        }

        static void CreateDiscordLobby(){
            Utils.WriteLine("Creating discord Lobby", MessageType.Info);
            //If we are the host create a discord lobby
            var txn = discord.GetLobbyManager().GetLobbyCreateTransaction();
            txn.SetCapacity((uint)QSBNetworkManager.singleton.maxConnections);
            txn.SetType(LobbyType.Public);

            discord.GetLobbyManager().CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobby) => {
                try{
                    if (result == Discord.Result.Ok){
                        lobbySecret = discord.GetLobbyManager().GetLobbyActivitySecret(lobby.Id);
                        Utils.WriteLine("Created lobby " + lobby.Id, MessageType.Success);
                        
                        DiscordManager.discord.GetLobbyManager().ConnectVoice(lobby.Id, (result) => {
                            if (result == Discord.Result.Ok){
                                Utils.WriteLine("Voice connected!");
                            }
                        });
                    }
                }catch(ResultException e){
                    Utils.WriteLine("Failed to create Lobby! Trying Again", MessageType.Error);
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
                    Utils.WriteLine("Sending Lobby Secret to " + info + " :: " + lobbySecret + ")", MessageType.Info);
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
            float maxVol = DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<float>("Global Volume");
            foreach (KeyValuePair<uint, long> playerKV in PlayerDiscordID){ //Explicit cause I'm bad at programming
                if (!QSBPlayerManager.PlayerExists(playerKV.Key))
                    continue;

                PlayerInfo player = QSBPlayerManager.GetPlayer(playerKV.Key);

                if (!player.IsReady)
                    continue;

                if (player.IsLocalPlayer)
                    continue;
                
                if (isSpeaking.TryGetValue(playerKV.Value, out bool speaking) && !speaking) //This works cause the TryGetValue gets evaluated first
                    continue;

                //Are we dead and is the other person dead?
                if (QSBPlayerManager.LocalPlayer.IsDead && player.IsDead){
                    discord.GetVoiceManager().SetLocalVolume(playerKV.Value, (byte)maxVol);
                    continue;
                }
                else if(!QSBPlayerManager.LocalPlayer.IsDead && player.IsDead){
                    discord.GetVoiceManager().SetLocalVolume(playerKV.Value, 0);
                    continue;
                }

                if (player.Camera == null || QSBPlayerManager.LocalPlayer.Camera == null)
                    continue;

                float dist = (player.Camera.transform.position - Camera.current.transform.position).magnitude;
                float bolume = CalcBolume(dist);

                if (DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<bool>("Scout Speaker")){
                    //Probe acts as speaker
                    if (player.ProbeBody != null){
                        float probeDist = (player.ProbeBody.transform.position - QSBPlayerManager.LocalPlayer.Camera.transform.position).magnitude;
                        bolume = Mathf.Max(bolume,CalcBolume(probeDist));
                    }
                }
                
                if (QSBPlayerManager.LocalPlayer == null){
                    Utils.WriteLine("LocalPlayer is null, How did we get here?", MessageType.Error);
                    continue;
                }

                #region SignalScope

                if(QSBPlayerManager.LocalPlayer.SignalscopeEquipped){
                    if (QSBPlayerManager.LocalPlayer.LocalSignalscope._strongestSignals == null)
                        continue;

                    if (Constants.PlayerSignals == null){
                        Utils.WriteLine("PlayerSignals is null, How did we get here?", MessageType.Error);
                        continue;
                    }

                    if (Constants.PlayerSignals.TryGetValue(player, out AudioSignal signal)){
                        bolume = Mathf.Max(bolume,Mathf.Clamp(maxVol *  signal._signalStrength, 0, maxVol));
                    }
                }
                
                #endregion
                
                if(DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<bool>("Debug Mode"))
                    Utils.WriteLine("bolume for " + player.Name + " : " + bolume + " | " + discord.GetVoiceManager().GetLocalVolume(playerKV.Key), MessageType.Info);
                
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
