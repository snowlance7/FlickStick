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

        // Configs
        public static ConfigEntry<string> configLevelRarities;
        public static ConfigEntry<string> configCustomLevelRarities;
        public static ConfigEntry<int> configMinValue;
        public static ConfigEntry<int> configMaxValue;

        public static ConfigEntry<bool> configEnableStore;
        public static ConfigEntry<int> configStorePrice;

        public static ConfigEntry<float> configPokeStunTime;
        public static ConfigEntry<float> configPokeForce;
        public static ConfigEntry<float> configFlickForce;
        public static ConfigEntry<float> configFlipOffRange;
        public static ConfigEntry<float> configPokeRange;
        public static ConfigEntry<float> configFlickRange;
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
            configMinValue = Config.Bind("Scrap", "Min Value", 50, "Min scrap value of the flickstick.");
            configMaxValue = Config.Bind("Scrap", "Max Value", 100, "Max scrap value of the flickstick.");

            configEnableStore = Config.Bind("Store", "Enable Store", true, "Whether or not the flickstick should be buyable in the store.");
            configStorePrice = Config.Bind("Store", "Store Price", 300, "Price of the flickstick in the store.");

            configPokeStunTime = Config.Bind("General", "Poke Stun Time", 1f, "Time the poke function stuns an enemy.");
            configPokeForce = Config.Bind("General", "Poke Force", 15f, "How much force is applied to a player when the poke is used.");
            configFlickForce = Config.Bind("General", "Flick Force", 30f, "How much force is applied to an enemy or player when the flick is used.");
            configFlipOffRange = Config.Bind("General", "Flipp Off Range", 30f, "How far the flip taunts the enemy");
            configPokeRange = Config.Bind("General", "Poke Range", 5f, "The range from the tip of the pointer that an enemy will be poked.");
            configFlickRange = Config.Bind("General", "Flick Range", 5f, "The range from the tip of the pointer that an enemy will be flicked.");

            // Loading Assets
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            ModAssets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), "flickstick_assets"));
            if (ModAssets == null)
            {
                Logger.LogError($"Failed to load custom assets.");
                return;
            }
            LoggerInstance.LogDebug($"Got AssetBundle at: {Path.Combine(sAssemblyLocation, "flickstick_assets")}");

            Item FlickStick = ModAssets.LoadAsset<Item>("Assets/ModAssets/FlickStickItem.asset");
            if (FlickStick == null) { LoggerInstance.LogError("Error: Couldnt get FlickStickItem from assets"); return; }
            LoggerInstance.LogDebug($"Got FlickStick prefab");

            FlickStick.minValue = configMinValue.Value;
            FlickStick.maxValue = configMaxValue.Value;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(FlickStick.spawnPrefab);
            LethalLib.Modules.Utilities.FixMixerGroups(FlickStick.spawnPrefab);
            LethalLib.Modules.Items.RegisterScrap(FlickStick, GetLevelRarities(configLevelRarities.Value), GetCustomLevelRarities(configCustomLevelRarities.Value));

            if (configEnableStore.Value)
            {
                LethalLib.Modules.Items.RegisterShopItem(FlickStick, configStorePrice.Value);
            }

            // Finished
            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        public Dictionary<Levels.LevelTypes, int> GetLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<Levels.LevelTypes, int> levelRaritiesDict = new Dictionary<Levels.LevelTypes, int>();

                if (levelsString != null && levelsString != "")
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (Enum.TryParse<Levels.LevelTypes>(levelType, out Levels.LevelTypes levelTypeEnum) && int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            levelRaritiesDict.Add(levelTypeEnum, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return levelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null!;
            }
        }

        public Dictionary<string, int> GetCustomLevelRarities(string levelsString)
        {
            try
            {
                Dictionary<string, int> customLevelRaritiesDict = new Dictionary<string, int>();

                if (levelsString != null)
                {
                    string[] levels = levelsString.Split(',');

                    foreach (string level in levels)
                    {
                        string[] levelSplit = level.Split(':');
                        if (levelSplit.Length != 2) { continue; }
                        string levelType = levelSplit[0].Trim();
                        string levelRarity = levelSplit[1].Trim();

                        if (int.TryParse(levelRarity, out int levelRarityInt))
                        {
                            customLevelRaritiesDict.Add(levelType, levelRarityInt);
                        }
                        else
                        {
                            LoggerInstance.LogError($"Error: Invalid level rarity: {levelType}:{levelRarity}");
                        }
                    }
                }
                return customLevelRaritiesDict;
            }
            catch (Exception e)
            {
                Logger.LogError($"Error: {e}");
                return null!;
            }
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
