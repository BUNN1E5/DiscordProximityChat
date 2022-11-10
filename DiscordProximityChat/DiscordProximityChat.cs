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

            //Thanks to QSB I dont need to make this file lmao
            //IMenuAPI menuAPI = ModHelper.Interaction.TryGetModApi<IMenuAPI>("_nebula.MenuFramework");

            //TODO :: ADD A VOLUME SLIDER

            //We do this to setup the Discord Manager for the first time
            //Use instance otherwise
            DiscordManager.Init();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) => {
                ModHelper.Events.Unity.RunWhen(() => QSBWorldSync.AllObjectsReady, () => {
                    SetupSignalScopes(scene, loadScene);
                });
            };

            QSBPlayerManager.OnAddPlayer += SetupPlayer;
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
            ModHelper.Events.Unity.RunWhen(() => playerInfo.IsReady && playerInfo.Body != null, () => {
                if (playerInfo.Body == null){
                    ModHelper.Console.WriteLine("How did you even get here?", MessageType.Error);
                    return;
                }

                TalkingAnimationManager.SetupTalkingHead(playerInfo); //Setup Talking Heads for Everyone

                if (playerInfo.IsLocalPlayer)
                    return;

                //Everything here is for only the remote players
                ModHelper.Console.WriteLine("Adding Audio Signal", MessageType.Success);
                
                //There is a check in the Bidirectional Dictionary
                Constants.PlayerSignals.Remove(playerInfo);
                
                AudioSignal signal = playerInfo.HudMarker.transform.gameObject.AddComponent<AudioSignal>();
                Constants.PlayerSignals.Add(playerInfo, signal);

                signal._frequency = SignalFrequency.Radio;
                if (!EnumUtils.IsDefined<SignalName>(playerInfo.Name)){
                    Constants.PlayerSignalNames.Add(playerInfo, EnumUtils.Create<SignalName>(playerInfo.Name));
                }
                
                signal._name = Constants.PlayerSignalNames[playerInfo];

                PlayerData._currentGameSave.knownSignals[(int) Constants.PlayerSignalNames[playerInfo]] = true;

                ModHelper.Console.WriteLine("Add the known signal for the local player", MessageType.Success);
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
