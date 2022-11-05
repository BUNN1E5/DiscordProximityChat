using System;
using Discord;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using QSB.Menus;
using QSB.Player;
using QSB.WorldSync;


namespace DiscordProximityChat
{
    public class DiscordProximityChat : ModBehaviour{
        public static DiscordProximityChat instance;

        private void Start(){
            instance = this;
            ModHelper.Console.WriteLine($"{nameof(DiscordProximityChat)} is loaded!", MessageType.Success);

            //Thanks to QSB I dont need to make this file lmao
            //ModHelper.Interaction.GetModApi<IMenuAPI>("_nebula.MenuFramework");
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

        public void FixedUpdate(){
            try{
                DiscordManager.RunCallbacks();
                DiscordManager.DiscordVolumeUpdater();
            } catch(ResultException e) {
                ModHelper.Console.WriteLine("Cant get CurrentUser : " + e, MessageType.Error);
            }
        }

        void SetupSignalScopes(OWScene scene, OWScene loadScene){
            if (loadScene != OWScene.SolarSystem) return;
            QSBPlayerManager.PlayerList.ForEach(SetupPlayer);
        }

        public void SetupPlayer(PlayerInfo playerInfo){
            if (playerInfo.IsLocalPlayer)
                return;
            
            ModHelper.Events.Unity.RunWhen(() => playerInfo.IsReady, () => {
                ModHelper.Console.WriteLine("Adding Audio Signal", MessageType.Success);
                AudioSignal signal = playerInfo.Body.AddComponent<AudioSignal>();
                signal._frequency = SignalFrequency.Traveler;
                if (!EnumUtils.IsDefined<SignalName>(playerInfo.Name)){
                    Constants.PlayerSignals.Add(playerInfo, EnumUtils.Create<SignalName>(playerInfo.Name));
                    Constants.ReversePlayerSignals.Add(Constants.PlayerSignals[playerInfo],  playerInfo);
                }

                signal._name = Constants.PlayerSignals[playerInfo];
                
                PlayerData._currentGameSave.knownSignals.Add((int)Constants.PlayerSignals[playerInfo], true);
                
                ModHelper.Console.WriteLine("Add the known signal for the local player", MessageType.Success);
            });
        }

        public void CleanUpSignal(PlayerInfo playerInfo){
            Constants.ReversePlayerSignals.Remove(Constants.PlayerSignals[playerInfo]);
            Constants.PlayerSignals.Remove(playerInfo);
            EnumUtils.Remove<SignalName>(playerInfo.Name);
        }
    }
}
