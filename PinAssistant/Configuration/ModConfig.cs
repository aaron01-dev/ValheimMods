using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;
using Text = WxAxW.PinAssistant.Configuration.TextAttribute;

namespace WxAxW.PinAssistant.Configuration
{
    internal class ModConfig
    {
        private ConfigFile Config;
        private static ModConfig m_instance;
        public static ModConfig Instance => m_instance;

        #region config vars

        private ConfigEntry<bool> m_isEnabledConfig;
        private ConfigEntry<bool> m_isAutoPinningEnabledConfig;
        private ConfigEntry<bool> m_isSearchWindowEnabledConfig;
        private ConfigEntry<float> m_lookDistanceConfig;
        private ConfigEntry<string> m_trackedObjectsConfig;
        private ConfigEntry<float> m_redundancyDistanceConfig;
        private ConfigEntry<float> m_tickRateConfig;

        private Dictionary<ConfigEntry<bool>, Type> m_typeDictionary = new Dictionary<ConfigEntry<bool>, Type>();
        private ConfigEntry<bool> m_trackTypeDestructibleConfig;    // a lot of things, objects that have other types may have this as well (ie Berry Bush - Pickable and Destructible)
        private ConfigEntry<bool> m_trackTypePickableConfig;        // things you can press to pick (flint, stone, branch, berry bush, mushroom, etc)
        private ConfigEntry<bool> m_trackTypeMineRockConfig;        // Minerals
        private ConfigEntry<bool> m_trackTypeLocationConfig;        // POIs: crypts, sunken crypts, structures that transport you to a different map
        private ConfigEntry<bool> m_trackTypeSpawnAreaConfig;       // Spawners: bone pile, etc.
        private ConfigEntry<bool> m_trackTypeVegvisirConfig;        // the runestone that shows you boss locations
        private ConfigEntry<bool> m_trackTypeResourceRootConfig;    // Yggdrasil root (the giant ancient root one)
        private ConfigEntry<bool> m_trackTypeTreeBaseConfig;        // Trees

        private ConfigEntry<KeyboardShortcut> m_trackLookedObjectConfig;
        private ConfigEntry<KeyboardShortcut> m_pinLookedObjectConfig;
        private ConfigEntry<KeyboardShortcut> m_toggleFilterWindowConfig;
        private ConfigEntry<KeyboardShortcut> m_reloadTrackedObjectsConfig;

        private ConfigEntry<bool> m_isDebugModeConfig;

        public ConfigEntry<bool> IsEnabledConfig { get => m_isEnabledConfig; set => m_isEnabledConfig = value; }
        public ConfigEntry<bool> IsAutoPinningEnabledConfig { get => m_isAutoPinningEnabledConfig; set => m_isAutoPinningEnabledConfig = value; }
        public ConfigEntry<bool> IsSearchWindowEnabledConfig { get => m_isSearchWindowEnabledConfig; set => m_isSearchWindowEnabledConfig = value; }
        public ConfigEntry<float> LookDistanceConfig { get => m_lookDistanceConfig; set => m_lookDistanceConfig = value; }
        public ConfigEntry<string> TrackedObjectsConfig { get => m_trackedObjectsConfig; set => m_trackedObjectsConfig = value; }
        public ConfigEntry<float> RedundancyDistanceConfig { get => m_redundancyDistanceConfig; set => m_redundancyDistanceConfig = value; }
        public ConfigEntry<float> TickRateConfig { get => m_tickRateConfig; set => m_tickRateConfig = value; }

        public Dictionary<ConfigEntry<bool>, Type> TypeDictionary { get => m_typeDictionary; set => m_typeDictionary = value; }
        public ConfigEntry<bool> IsDebugModeConfig { get => m_isDebugModeConfig; set => m_isDebugModeConfig = value; }
        public ConfigEntry<bool> TrackTypeDestructibleConfig { get => m_trackTypeDestructibleConfig; set => m_trackTypeDestructibleConfig = value; }
        public ConfigEntry<bool> TrackTypePickableConfig { get => m_trackTypePickableConfig; set => m_trackTypePickableConfig = value; }
        public ConfigEntry<bool> TrackTypeMineRockConfig { get => m_trackTypeMineRockConfig; set => m_trackTypeMineRockConfig = value; }
        public ConfigEntry<bool> TrackTypeLocationConfig { get => m_trackTypeLocationConfig; set => m_trackTypeLocationConfig = value; }
        public ConfigEntry<bool> TrackTypeSpawnAreaConfig { get => m_trackTypeSpawnAreaConfig; set => m_trackTypeSpawnAreaConfig = value; }
        public ConfigEntry<bool> TrackTypeVegvisirConfig { get => m_trackTypeVegvisirConfig; set => m_trackTypeVegvisirConfig = value; }
        public ConfigEntry<bool> TrackTypeResourceRootConfig { get => m_trackTypeResourceRootConfig; set => m_trackTypeResourceRootConfig = value; }
        public ConfigEntry<bool> TrackTypeTreeBaseConfig { get => m_trackTypeTreeBaseConfig; set => m_trackTypeTreeBaseConfig = value; }

        public ConfigEntry<KeyboardShortcut> TrackLookedObjectConfig { get => m_trackLookedObjectConfig; set => m_trackLookedObjectConfig = value; }
        public ConfigEntry<KeyboardShortcut> PinLookedObjectConfig { get => m_pinLookedObjectConfig; set => m_pinLookedObjectConfig = value; }
        public ConfigEntry<KeyboardShortcut> ToggleFilterWindowConfig { get => m_toggleFilterWindowConfig; set => m_toggleFilterWindowConfig = value; }
        public ConfigEntry<KeyboardShortcut> ReloadTrackedObjectsConfig { get => m_reloadTrackedObjectsConfig; set => m_reloadTrackedObjectsConfig = value; }

        #endregion config vars

        internal static void Init(ConfigFile configFile)
        {
            if (m_instance != null) return;
            m_instance = new ModConfig();

            m_instance.SetupValues(configFile);
        }

        private void SetupValues(ConfigFile configFile)
        {
            Config = configFile;

            // bind the variables to the assigned configs
            m_isEnabledConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_GENERAL),
                Text.Get(TextType.CONFIG_NAME_TOGGLE_MOD),
                true,
                new ConfigDescription(
                    Text.Get(
                        TextType.CONFIG_MESSAGE_TOGGLE_MOD,
                        Text.Get(TextType.CONFIG_NAME_TOGGLE_AUTOPINNING)
                        ),
                    null,
                    new ConfigurationManagerAttributes { Order = 6 })
                );
            m_isAutoPinningEnabledConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_GENERAL),
                Text.Get(TextType.CONFIG_NAME_TOGGLE_AUTOPINNING),
                true,
                new ConfigDescription(
                    Text.Get(
                        TextType.CONFIG_MESSAGE_TOGGLE_AUTOPINNING,
                        Text.Get(TextType.CONFIG_NAME_KEY_TRACKOBJECT)
                        ),
                    null,
                    new ConfigurationManagerAttributes { Order = 5 })
                );
            m_isSearchWindowEnabledConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_GENERAL),
                Text.Get(TextType.CONFIG_NAME_TOGGLE_STARTFILTERENABLED),
                true,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TOGGLE_STARTFILTERENABLED),
                    null,
                    new ConfigurationManagerAttributes { Order = 4 })
                );

            m_tickRateConfig = Config.Bind<float>(
                Text.Get(TextType.CONFIG_CATEGORY_GENERAL),
                Text.Get(TextType.CONFIG_NAME_VAL_TICKRATE),
                1f,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_VAL_TICKRATE),
                    null,
                    new ConfigurationManagerAttributes { Order = 3 })
                );
            m_redundancyDistanceConfig = Config.Bind<float>(
                Text.Get(TextType.CONFIG_CATEGORY_GENERAL),
                Text.Get(TextType.CONFIG_NAME_VAL_DISTANCEREDUNDANCY),
                20f,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_VAL_DISTANCEREDUNDANCY),
                    null,
                    new ConfigurationManagerAttributes { Order = 2 })
                );
            m_lookDistanceConfig = Config.Bind<float>(
                Text.Get(TextType.CONFIG_CATEGORY_GENERAL),
                Text.Get(TextType.CONFIG_NAME_VAL_DISTANCELOOK),
                25f,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_VAL_DISTANCELOOK),
                    null,
                    new ConfigurationManagerAttributes { Order = 1 })
                );

            m_trackTypeDestructibleConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_DESTRUCTIBLE),
                true,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_DESTRUCTIBLE),
                    null,
                    new ConfigurationManagerAttributes { Order = 8 })
                );
            m_trackTypePickableConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_PICKABLE),
                true,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_PICKABLE),
                    null,
                    new ConfigurationManagerAttributes { Order = 7 })
                );
            m_trackTypeMineRockConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_MINEROCK),
                true,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_MINEROCK),
                    null,
                    new ConfigurationManagerAttributes { Order = 6 })
                );
            m_trackTypeLocationConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_LOCATION),
                true,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_LOCATION),
                    null,
                    new ConfigurationManagerAttributes { Order = 5 })
                );
            m_trackTypeSpawnAreaConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_SPAWNAREA),
                true,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_SPAWNAREA),
                    null,
                    new ConfigurationManagerAttributes { Order = 4 })
                );
            m_trackTypeVegvisirConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_VEGVISIR),
                false,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_VEGVISIR),
                    null,
                    new ConfigurationManagerAttributes { Order = 3 })
                );
            m_trackTypeResourceRootConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_RESOURCEROOT),
                false,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_RESOURCEROOT),
                    null,
                    new ConfigurationManagerAttributes { Order = 2 })
                );
            m_trackTypeTreeBaseConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TYPES),
                Text.Get(TextType.CONFIG_NAME_TYPE_TREEBASE),
                false,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_TYPE_TREEBASE),
                    null,
                    new ConfigurationManagerAttributes { Order = 1 })
                );

            m_typeDictionary.Add(m_trackTypeDestructibleConfig, typeof(Destructible));
            m_typeDictionary.Add(m_trackTypePickableConfig, typeof(Pickable));
            m_typeDictionary.Add(m_trackTypeMineRockConfig, typeof(MineRock));
            m_typeDictionary.Add(m_trackTypeLocationConfig, typeof(Location));
            m_typeDictionary.Add(m_trackTypeSpawnAreaConfig, typeof(SpawnArea));
            m_typeDictionary.Add(m_trackTypeVegvisirConfig, typeof(Vegvisir));
            m_typeDictionary.Add(m_trackTypeResourceRootConfig, typeof(ResourceRoot));
            m_typeDictionary.Add(m_trackTypeTreeBaseConfig, typeof(TreeBase));

            m_trackLookedObjectConfig = Config.Bind(
                Text.Get(TextType.CONFIG_CATEGORY_HOTKEYS),
                Text.Get(TextType.CONFIG_NAME_KEY_TRACKOBJECT),
                new KeyboardShortcut(KeyCode.T, KeyCode.LeftControl),
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_KEY_TRACKOBJECT),
                    null,
                    new ConfigurationManagerAttributes { Order = 4 })
                );
            m_pinLookedObjectConfig = Config.Bind(
                Text.Get(TextType.CONFIG_CATEGORY_HOTKEYS),
                Text.Get(TextType.CONFIG_NAME_KEY_PINOBJECT),
                new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl),
                new ConfigDescription(
                    Text.Get(
                        TextType.CONFIG_MESSAGE_KEY_PINOBJECT,
                        Text.Get(TextType.CONFIG_NAME_TOGGLE_AUTOPINNING),
                        Text.Get(TextType.CONFIG_NAME_KEY_TRACKOBJECT)
                        ),
                    null,
                    new ConfigurationManagerAttributes { Order = 3 })
                );
            m_toggleFilterWindowConfig = Config.Bind(
                Text.Get(TextType.CONFIG_CATEGORY_HOTKEYS),
                Text.Get(TextType.CONFIG_NAME_KEY_TOGGLEFILTERWINDOW),
                new KeyboardShortcut(KeyCode.Tab),
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_KEY_TOGGLEFILTERWINDOW),
                    null,
                    new ConfigurationManagerAttributes { Order = 2 })
                );
            m_reloadTrackedObjectsConfig = Config.Bind(
                Text.Get(TextType.CONFIG_CATEGORY_HOTKEYS),
                Text.Get(TextType.CONFIG_NAME_KEY_RELOADTRACKED),
                new KeyboardShortcut(),
                new ConfigDescription(
                    Text.Get(
                        TextType.CONFIG_MESSAGE_KEY_RELOADTRACKED,
                        Text.Get(TextType.CONFIG_NAME_OBJEECTSTRACKED)
                        ),
                    null,
                    new ConfigurationManagerAttributes { Order = 1 })
                );

            m_isDebugModeConfig = Config.Bind<bool>(
                Text.Get(TextType.CONFIG_CATEGORY_TECHNICAL),
                Text.Get(TextType.CONFIG_NAME_DEBUGMODE),
                false,
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_DEBUGMODE),
                    null,
                    new ConfigurationManagerAttributes { Order = 2 })
                );
            m_trackedObjectsConfig = Config.Bind<string>(
                Text.Get(TextType.CONFIG_CATEGORY_TECHNICAL),
                Text.Get(TextType.CONFIG_NAME_OBJEECTSTRACKED), "",
                new ConfigDescription(
                    Text.Get(
                        TextType.CONFIG_MESSAGE_OBJECTSTRACKED,
                        Text.Get(TextType.CONFIG_NAME_KEY_PINOBJECT)
                        ),
                    null,
                    new ConfigurationManagerAttributes { Order = 1 })
                );
        }
    }
}