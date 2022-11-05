using System;
using System.Collections.Generic;
using Discord;
using OWML.Common;
using OWML.Logging;
using OWML.ModHelper;
using QSB.Menus;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.PlayerBodySetup.Remote;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace DiscordProximityChat
{
    public class DiscordProximityChat : ModBehaviour{
        public static DiscordProximityChat instance;

        private void Start(){
            instance = this;
            ModHelper.Console.WriteLine($"{nameof(DiscordProximityChat)} is loaded!", MessageType.Success);

            //Thanks to QSB I dont need to make this file lmao
            ModHelper.Interaction.GetModApi<IMenuAPI>("_nebula.MenuFramework");

            //We do this to setup the Discord Manager for the first time
            //Use instance otherwise
            DiscordManager discordManager = new DiscordManager();
        }

        public void FixedUpdate(){
            try{
                DiscordManager.instance.RunCallbacks();
                //DiscordManager.instance.DiscordVolumeUpdater();
                //ModHelper.Console.WriteLine("UserID :: " + discordManager.getUserID(), MessageType.Success);
            } catch(ResultException e) {
                ModHelper.Console.WriteLine("Cant get CurrentUser : " + e, MessageType.Error);
            }
        }
    }
}
