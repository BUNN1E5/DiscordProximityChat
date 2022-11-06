using System;
using UnityEngine;

namespace DiscordProximityChat{
    public class TalkingAnimationController : MonoBehaviour{
        public bool IsTalking;

        private float animationSpeed = 25;
        private float animationAmplitude = 0.005f;

        public static TalkingAnimationController Create(Transform transform){
            var existingComponent = transform.GetComponent<TalkingAnimationController>();
            return existingComponent != null
                ? existingComponent
                : transform.gameObject.AddComponent<TalkingAnimationController>();
        }

        private void Update(){
            if (IsTalking){
                var animationTime = Time.time * animationSpeed;
                var multiplier = animationSpeed * animationAmplitude;
                var offset = 1 - animationAmplitude * 0.5f;
                var x = Mathf.Sin(animationTime) * multiplier + offset;
                var z = Mathf.Cos(animationTime) * multiplier + offset;
                transform.localScale = new Vector3(x, 1, z);
            }
            else{
                transform.localScale = Vector3.one;
            }
        }
    }
}