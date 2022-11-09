using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

namespace DiscordProximityChat{
    public class Utils{

        public static byte CalculateProximityVolume(float x, float a, float b){
            //Function is 1/(x^a + 1)^(b*b)
            float val = 1/Mathf.Pow(Mathf.Pow(x, a) + 1, b*b);
            return (byte)(200 * val);
        }
        
        public static byte CalculateProximityVolume(float x, float a, float b, bool clip){
            //Function is 1/(x^a + 1)^(b*b)
            float val = 1/Mathf.Pow(Mathf.Pow(x, a) + 1, b*b);
            val = clip && val <= 0 ? 0 : val;
            return (byte)(200 * val);
        }
    }
}