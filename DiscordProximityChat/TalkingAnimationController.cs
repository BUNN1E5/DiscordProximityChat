using System;
using System.Collections.Generic;
using OWML.ModHelper;
using QSB.Player;
using UnityEngine;

namespace DiscordProximityChat{

    public static class TalkingAnimationManager{
        //Just in case we need to ever actually work with this
        //public static Dictionary<uint, TalkingAnimationController> controllers = new();
        
        public static void SetupTalkingHead(PlayerInfo playerInfo)
        {
            if (playerInfo.Body == null)
                return;

            // if (controllers.ContainsKey(playerInfo.PlayerId))
            //     return;

            var root = playerInfo.Body.transform.Find(playerInfo.IsLocalPlayer ? "Traveller_HEA_Player_v2" : "REMOTE_Traveller_HEA_Player_v2");

            if (root == null)
                return;
            
            var playerHead = root.Find(
                "Traveller_Rig_v01:Traveller_Trajectory_Jnt/Traveller_Rig_v01:Traveller_ROOT_Jnt/Traveller_Rig_v01:Traveller_Spine_01_Jnt/Traveller_Rig_v01:Traveller_Spine_02_Jnt/Traveller_Rig_v01:Traveller_Spine_Top_Jnt/Traveller_Rig_v01:Traveller_Neck_01_Jnt/Traveller_Rig_v01:Traveller_Neck_Top_Jnt");

            if (playerHead == null)
                return;

            DiscordManager.PlayerDiscordID.TryGetValue(playerInfo.PlayerId, out long discordId);

            if (discordId == default)
                return;
            
            DiscordProximityChat.instance.ModHelper.Console.WriteLine($"Everything seems OK {(playerInfo.IsLocalPlayer ? "local" : "remote")}");

            Create(playerHead, playerInfo, discordId);
        }
        
        public static TalkingAnimationController Create(Transform transform, PlayerInfo info, long discordId){
            var existingComponent = transform.GetComponent<TalkingAnimationController>();
            existingComponent = existingComponent != null ? existingComponent : transform.gameObject.AddComponent<TalkingAnimationController>();
            existingComponent.discordID = discordId;
            if (!info.IsLocalPlayer){
                existingComponent.HUDMarker = info.HudMarker;
                existingComponent.MapMarker = info.Body.GetComponent<PlayerMapMarker>();
            }
            return existingComponent;
        }
    }

    public class TalkingAnimationController : MonoBehaviour{
        public float animationSpeed = 25;
        public float animationAmplitude = 0.005f;
        public long discordID;

        public PlayerHUDMarker HUDMarker;
        private MeshRenderer OnScreenMarker;
        private MeshRenderer OffScreenMarker;
        
        public PlayerMapMarker MapMarker;

        private void Start(){
            if (HUDMarker == null){
                OnScreenMarker = HUDMarker._canvasMarker._marker;
                OffScreenMarker = HUDMarker._canvasMarker._offScreenIndicator.GetComponent<MeshRenderer>();
            }
        }

        private void Update(){
            if (!DiscordManager.isSpeaking.ContainsKey(discordID)){
                transform.localScale = Vector3.one;
                return;
            }
            
            if (DiscordManager.isSpeaking[discordID]){
                var animationTime = Time.time * animationSpeed;
                var multiplier = animationSpeed * animationAmplitude;
                var offset = 1 - animationAmplitude * 0.5f;
                var x = Mathf.Sin(animationTime) * multiplier + offset;
                var z = Mathf.Cos(animationTime) * multiplier + offset;
                if (DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<bool>("Player Head Bobbing")){
                    transform.localScale = new Vector3(x, 1, z);
                }

                if (DiscordProximityChat.instance.ModHelper.Config.GetSettingsValue<bool>("Player HUD Icon Bobbing")){
                    if (OnScreenMarker != null){
                        OnScreenMarker.transform.localScale = new Vector3(x, 1, z);
                        OnScreenMarker.material.color = new Color(0, x, 0);
                    }

                    if (OffScreenMarker != null){
                        OffScreenMarker.transform.localScale = new Vector3(x, 1, z);
                        OffScreenMarker.material.color = new Color(0, x, 0);
                    }
                }
            } else {
                transform.localScale = Vector3.one;
                
                if (OnScreenMarker != null){
                    OnScreenMarker.transform.localScale = Vector3.one;
                    OnScreenMarker.material.color = new Color(1, 1, 1, 1);
                }
                
                if (OffScreenMarker != null){
                    OffScreenMarker.transform.localScale = Vector3.one;
                    OffScreenMarker.material.color = new Color(1, 1, 1, 1);
                }
            }
        }
    }
}