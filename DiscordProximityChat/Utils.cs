using System;
using System.Collections;
using System.Collections.Generic;
using OWML.Common;
using UnityEngine.InputSystem;
using UnityEngine;

namespace DiscordProximityChat{
    public class Utils{

        public static IModConfig Config = DiscordProximityChat.instance.ModHelper.Config;
        public static float CalculateProximityVolume(float x, float a, float b){
            //Function is 1/(x^a + 1)^(b*b)
            return 1/Mathf.Pow(Mathf.Pow(x, a) + 1, b*b);
        }
        
        public static float CalculateProximityVolume(float x, float a, float b, bool clip){
            //Function is 1/(x^a + 1)^(b*b)
            float val = 1/Mathf.Pow(Mathf.Pow(x, a) + 1, b*b);
            return clip && val <= 0 ? 0 : val;
        }

        public static void WriteLine(string line, MessageType type){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine(line, type);
        }
        
        public static void WriteLine(string line){
            DiscordProximityChat.instance.ModHelper.Console.WriteLine(line);
        }
        
        public static Coroutine RunWhen(Func<bool> predicate, Action action, Coroutine c){
            DiscordProximityChat.instance.StopCoroutine(c);
            return RunWhen(predicate, action);
        }

        public static Coroutine RunWhen(Func<bool> predicate, Action action) => 
            DiscordProximityChat.instance.StartCoroutine(WaitUntil(predicate, action));

        private static IEnumerator WaitUntil(Func<bool> predicate, Action action){
            yield return new WaitUntil(predicate);
            action();
        }
    }
}