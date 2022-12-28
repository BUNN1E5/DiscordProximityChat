using System;
using System.Reflection;
using Discord;
using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using QSB;
using QSB.Menus;
using QSB.Player;
using QSB.WorldSync;
using UnityEngine;


namespace DiscordProximityChat
{
    public class DiscordProximityChat : ModBehaviour{
        public static DiscordProximityChat instance;

        public static bool useHeadBobbing = true;
        
        private void Start(){
            instance = this;
            ModHelper.Console.WriteLine($"{nameof(DiscordProximityChat)} is loaded!", MessageType.Success);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            
            DiscordManager.Init();
            
            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => {
                Utils.RunWhen(() => QSBWorldSync.AllObjectsReady, () => {
                    SetupSignalScopes(scene, loadScene);
                });
            };

            //TODO :: ADD OWO SUPPORT
            QSBPlayerManager.OnAddPlayer += SetupPlayer; //Change to 
            QSBPlayerManager.OnRemovePlayer += CleanUpSignal;
        }

        public void Update(){
            DiscordManager.RunCallbacks();
            DiscordManager.DiscordVolumeUpdater();
        }

        public void LateUpdate(){
            DiscordManager.RunCallbacks();
            DiscordManager.discord.GetNetworkManager().Flush();
        }

        void SetupSignalScopes(OWScene scene, OWScene loadScene){
            if (!loadScene.IsUniverseScene()) return;
            QSBPlayerManager.PlayerList.ForEach(SetupPlayer);
        }

        public void SetupPlayer(PlayerInfo playerInfo)
        {
            Utils.RunWhen(() => playerInfo.IsReady && playerInfo.Camera != null, () => {
                if (playerInfo.Camera == null){
                    Utils.WriteLine("How did you even get here?", MessageType.Error);
                    return;
                }

                TalkingAnimationManager.SetupTalkingHead(playerInfo); //Setup Talking Heads for Everyone

                if (playerInfo.IsLocalPlayer)
                    return;

                //Everything here is for only the remote players
                ModHelper.Console.WriteLine("Adding Audio Signal", MessageType.Success);
                
                //There is a check in the Bidirectional Dictionary
                Constants.PlayerSignals.Remove(playerInfo);
                
                AudioSignal signal = playerInfo.Camera.transform.gameObject.AddComponent<AudioSignal>();
                Constants.PlayerSignals.Add(playerInfo, signal);

                signal._frequency = SignalFrequency.Traveler;
                if (!EnumUtils.IsDefined<SignalName>(playerInfo.Name)){
                    Constants.PlayerSignalNames.Add(playerInfo, EnumUtils.Create<SignalName>(playerInfo.Name));
                }
                
                signal._name = Constants.PlayerSignalNames[playerInfo];

                PlayerData._currentGameSave.knownSignals[(int) Constants.PlayerSignalNames[playerInfo]] = true;

                Utils.WriteLine("Add the known signal for the local player", MessageType.Success);
            });
        }

        public void CleanUpSignal(PlayerInfo playerInfo){
            if (Constants.PlayerSignals.TryGetValue(playerInfo, out AudioSignal signal)){
                GameObject.DestroyImmediate(signal);
                Constants.PlayerSignals.Remove(playerInfo);
            }
            
            if( Constants.PlayerSignalNames.Contains(playerInfo))
                Constants.PlayerSignalNames.Remove(playerInfo);
            
            if(EnumUtils.IsDefined<SignalName>(playerInfo.Name))
                EnumUtils.Remove<SignalName>(playerInfo.Name);
        }
    }
}
