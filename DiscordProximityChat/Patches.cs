using System.Linq;
using HarmonyLib;

namespace DiscordProximityChat{
    [HarmonyPatch]
    public class Patches{
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AudioSignal), nameof(AudioSignal.SignalNameToString))]
        public static bool SignalNameToStringPatch(SignalName name, ref string __result){
            if (Constants.PlayerSignals.ContainsKey(name)){
                __result = Constants.PlayerSignals[name].Name;
                return false;
            }
            return true;
        }
    }
}