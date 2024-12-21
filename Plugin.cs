using AmazingAssets.TerrainToMesh;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Extras;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace FlickStick
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        public static PlayerControllerB localPlayer { get { return GameNetworkManager.Instance.localPlayerController; } }
        public static bool IsServerOrHost { get { return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost; } }
        public static PlayerControllerB PlayerFromId(ulong id) { return StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.ClientPlayerList[id]]; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static Plugin PluginInstance;
        public static ManualLogSource LoggerInstance;

        public static AssetBundle ModAssets;

        public static AnimationClip PlayerGrabAnimation;
        public static AnimationClip PlayerChargeAnimation;
        public static AnimationClip PlayerChargeAnimation2;

        // Configs
        public static ConfigEntry<string> configLevelRarities;
        public static ConfigEntry<string> configCustomLevelRarities;
        public static ConfigEntry<string> configMinValue;
        public static ConfigEntry<string> configMaxValue;

        public static ConfigEntry<string> configEnableStore;
        public static ConfigEntry<string> configStorePrice;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


        private void Awake()
        {
            if (PluginInstance == null)
            {
                PluginInstance = this;
            }

            LoggerInstance = PluginInstance.Logger;

            harmony.PatchAll();

            InitializeNetworkBehaviours();

            // Configs

            // General
            
            configLevelRarities = Config.Bind("Scrap", "Level Rarities", "ExperimentationLevel:10, AssuranceLevel:10, VowLevel:10, OffenseLevel:30, AdamanceLevel:50, MarchLevel:50, RendLevel:50, DineLevel:50, TitanLevel:80, ArtificeLevel:80, EmbrionLevel:100, Modded:30", "Rarities for each level. See default for formatting. Leave blank to prevent spawning as scrap.");
            configCustomLevelRarities = Config.Bind("Scrap", "Custom Level Rarities", "", "Rarities for modded levels. Same formatting as level rarities.");
            configMinValue = Config.Bind("Scrap", "", "", "");
            configMaxValue = Config.Bind("Scrap", "", "", "");

            configEnableStore = Config.Bind("Store", "", "", "");
            configStorePrice = Config.Bind("Store", "", "", "");

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "flickstick_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "flickstick_assets")}");

            PlayerGrabAnimation = ModAssets.LoadAsset<AnimationClip>("Assets/ModAssets/Animations/GrabStick.anim");
            PlayerChargeAnimation = ModAssets.LoadAsset<AnimationClip>("Assets/ModAssets/Animations/ChargeStick.anim");
            PlayerChargeAnimation2 = ModAssets.LoadAsset<AnimationClip>("Assets/ModAssets/Animations/ChargeStick2.anim");

            Item FlickStick = ModAssets.LoadAsset<Item>("Assets/ModAssets/FlickStickItem.asset");
            if (FlickStick == null) { LoggerInstance.LogError("Error: Couldnt get FlickStickItem from assets"); return; }
            LoggerInstance.LogDebug($"Got FlickStick prefab");

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(FlickStick.spawnPrefab);
            LethalLib.Modules.Utilities.FixMixerGroups(FlickStick.spawnPrefab);
            LethalLib.Modules.Items.RegisterItem(FlickStick);
            //Animation.instantiateAnimations();
            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        private static void InitializeNetworkBehaviours()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            LoggerInstance.LogDebug("Finished initializing network behaviours");
        }
    }
}
