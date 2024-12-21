using BepInEx.Logging;
using HarmonyLib;
using static FlickStick.Plugin;

/* bodyparts
 * 0 head
 * 1 right arm
 * 2 left arm
 * 3 right leg
 * 4 left leg
 * 5 chest
 * 6 feet
 * 7 right hip
 * 8 crotch
 * 9 left shoulder
 * 10 right shoulder */

namespace FlickStick
{
    [HarmonyPatch]
    internal class TESTING
    {
        private static ManualLogSource logger = Plugin.LoggerInstance;
        static bool toggle;

        [HarmonyPostfix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.PingScan_performed))]
        public static void PingScan_performedPostFix()
        {

        }

        /*[HarmonyPostfix, HarmonyPatch(typeof(UnityEngine.Animator), nameof(UnityEngine.Animator.SetBool))]
        public static void SetBoolPostfix(string name, bool value)
        {
            logger.LogDebug(name + ": " + value);
        }*/

        [HarmonyPrefix, HarmonyPatch(typeof(HUDManager), nameof(HUDManager.SubmitChat_performed))]
        public static void SubmitChat_performedPrefix(HUDManager __instance)
        {
            string msg = __instance.chatTextField.text;
            string[] args = msg.Split(" ");

            switch (args[0])
            {
                case "/refresh":
                    RoundManager.Instance.RefreshEnemiesList();
                    HoarderBugAI.RefreshGrabbableObjectsInMapList();
                    break;
                default:
                    break;
            }
        }
    }
}