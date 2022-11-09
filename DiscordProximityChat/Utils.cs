using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace DiscordProximityChat{
    public class Utils{
        //sq = x^2, yes pass in an already squared x
        public static byte CalculateProximityVolume(float sqx){
            //Function is 1/(x^2 + 1)
            return (byte)(128 / (float) ((sqx) + 1));
        }
    }
}