﻿using BepInEx.Configuration;
using UnityEngine;
using Text = WxAxW.PinAssistant.TextAttribute;

namespace WxAxW.PinAssistant.Configuration
{
    internal class ModConfig
    {
        private ConfigFile Config;
        private static ModConfig m_instance;
        public static ModConfig Instance => m_instance ?? (m_instance = new ModConfig());

        #region config vars

        private ConfigEntry<bool> m_isEnabledConfig;
        private ConfigEntry<bool> m_isAutoPinningEnabledConfig;
        private ConfigEntry<float> m_lookDistanceConfig;
        private ConfigEntry<string> m_trackedObjectsConfig;
        private ConfigEntry<float> m_redundancyDistanceConfig;
        private ConfigEntry<float> m_tickRateConfig;

        private ConfigEntry<KeyboardShortcut> m_trackLookedObjectConfig;
        private ConfigEntry<KeyboardShortcut> m_pinLookedObjectConfig;
        private ConfigEntry<KeyboardShortcut> m_reloadTrackedObjectsConfig;

        private ConfigEntry<bool> m_isDebugModeConfig;

        public ConfigEntry<bool> IsEnabledConfig { get => m_isEnabledConfig; set => m_isEnabledConfig = value; }
        public ConfigEntry<bool> IsAutoPinningEnabledConfig { get => m_isAutoPinningEnabledConfig; set => m_isAutoPinningEnabledConfig = value; }
        public ConfigEntry<float> LookDistanceConfig { get => m_lookDistanceConfig; set => m_lookDistanceConfig = value; }
        public ConfigEntry<string> TrackedObjectsConfig { get => m_trackedObjectsConfig; set => m_trackedObjectsConfig = value; }
        public ConfigEntry<float> RedundancyDistanceConfig { get => m_redundancyDistanceConfig; set => m_redundancyDistanceConfig = value; }
        public ConfigEntry<float> TickRateConfig { get => m_tickRateConfig; set => m_tickRateConfig = value; }

        public ConfigEntry<KeyboardShortcut> TrackLookedObjectConfig { get => m_trackLookedObjectConfig; set => m_trackLookedObjectConfig = value; }
        public ConfigEntry<KeyboardShortcut> PinLookedObjectConfig { get => m_pinLookedObjectConfig; set => m_pinLookedObjectConfig = value; }
        public ConfigEntry<KeyboardShortcut> ReloadTrackedObjectsConfig { get => m_reloadTrackedObjectsConfig; set => m_reloadTrackedObjectsConfig = value; }

        public ConfigEntry<bool> IsDebugModeConfig { get => m_isDebugModeConfig; set => m_isDebugModeConfig = value; }

        #endregion config vars

        internal void Init(ConfigFile configFile)
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
                    new ConfigurationManagerAttributes { Order = 5 })
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
                Text.Get(TextType.CONFIG_NAME_VAL_DISTANCEREDUNDANCY), 30f,
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

            m_trackLookedObjectConfig = Config.Bind(
                Text.Get(TextType.CONFIG_CATEGORY_HOTKEYS),
                Text.Get(TextType.CONFIG_NAME_KEY_TRACKOBJECT),
                new KeyboardShortcut(KeyCode.T, KeyCode.LeftControl),
                new ConfigDescription(
                    Text.Get(TextType.CONFIG_MESSAGE_KEY_TRACKOBJECT),
                    null,
                    new ConfigurationManagerAttributes { Order = 3 })
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