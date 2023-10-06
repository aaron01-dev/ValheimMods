using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WxAxW.PinAssistant.Components;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Core;
using WxAxW.PinAssistant.Utils;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.WxAxW" + "." + PluginName;
        public const string PluginName = "PinAssistant";
        public const string PluginVersion = "1.2.2";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        public static Plugin m_instance;
        public bool m_isInGame = false; // variable to check if in game

        private readonly Harmony harmony = new Harmony(PluginGUID);

        private Coroutine m_coroutineAutoPin; // routine to start looking

        //private int m_layersToCheck = LayerMask.GetMask(Enumerable.Range(0, 31).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToArray()); // get all layers
        public static Plugin Instance => m_instance;

        private AssetBundle m_assetBundle;

        private void Awake()
        {
            if (m_instance == null) m_instance = this;
            harmony.PatchAll();
            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
            ModConfig.Instance.Init(Config);

            m_assetBundle = AssetUtils.LoadAssetBundleFromResources("pin_assistant");
            ModConfig.Instance.IsEnabledConfig.SettingChanged += OnTogglePluginConfig;    // add permanent listener to mod toggle
            SceneManager.sceneLoaded += OnSceneChange;  // subscribe regardless if in main menu or in game or whatever
            SceneManager.sceneLoaded += TMPGUIManager.Instance.Init;

            // initialize ui after event loads
            GUIManager.OnCustomGUIAvailable += LoadTrackObjectUI;
            MinimapManager.OnVanillaMapAvailable += LoadMinimapFilterUI;

            CheckScene();   // check current scene
            ModToggle();
            Debug.Log(TextType.PLUGIN_ENABLED);
        }

        private void Update()
        {
            if (ModConfig.Instance.TrackLookedObjectConfig.Value.IsDown())
            {
                GameObject obj = PinAssistantScript.Instance.LookAt(ModConfig.Instance.LookDistanceConfig.Value);
                TrackObjectUI.Instance?.SetupTrackObject(obj);
            }

            if (ModConfig.Instance.PinLookedObjectConfig.Value.IsDown())
                PinAssistantScript.Instance.PinLookedObject(ModConfig.Instance.LookDistanceConfig.Value, ModConfig.Instance.RedundancyDistanceConfig.Value);

            if (ModConfig.Instance.ReloadTrackedObjectsConfig.Value.IsDown())
                //AutoPinning.Instance.TrackLookedObjectToAutoPin(ModConfig.Instance.LookDistanceConfig.Value);
                PinAssistantScript.Instance.DeserializeTrackedObjects(ModConfig.Instance.TrackedObjectsConfig.Value);
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            // unsubscribe loose listeners
            ModConfig.Instance.IsEnabledConfig.SettingChanged -= OnTogglePluginConfig;    // add permanent listener to mod toggle
            SceneManager.sceneLoaded -= OnSceneChange;  // subscribe regardless if in main menu or in game or whatever
            SceneManager.sceneLoaded -= TMPGUIManager.Instance.Init;
            GUIManager.OnCustomGUIAvailable -= LoadTrackObjectUI;
            MinimapManager.OnVanillaMapAvailable -= LoadMinimapFilterUI;
            Debug.Log(TextType.PLUGIN_DISABLED);
        }

        private IEnumerator StartAutoPinCoroutine()
        {
            while (true)
            {
                PinAssistantScript.Instance.PinLookedObject(ModConfig.Instance.LookDistanceConfig.Value, ModConfig.Instance.RedundancyDistanceConfig.Value);
                yield return new WaitForSeconds(ModConfig.Instance.TickRateConfig.Value);
            }
        }

        private void ModToggle()
        {
            // is the config enabled and you're in game?
            bool valid = ModConfig.Instance.IsEnabledConfig.Value && (m_isInGame || ModConfig.Instance.IsDebugModeConfig.Value);
            enabled = valid;
        }

        private void OnEnable()
        {
            Debug.Log(TextType.MOD_ENABLED);
            List<Type> registeredTypes = new List<Type>();
            // add types
            foreach (var kvp in ModConfig.Instance.TypeDictionary)
            {
                // check if type is allowed to be found
                if (kvp.Key.Value) registeredTypes.Add(kvp.Value);
            }
            PinAssistantScript.Init(ModConfig.Instance.TrackedObjectsConfig.Value, registeredTypes); // initialize pinassistant with saved tracked objects and registered types
            PinAssistantScript.Instance.ModifiedTrackedObjects += OnNewTrackedObject;
            if (TrackObjectUI.Instance != null) TrackObjectUI.Instance.enabled = true;
            if (FilterPinsUI.Instance != null) FilterPinsUI.Instance.enabled = true;
            ModConfig.Instance.IsAutoPinningEnabledConfig.SettingChanged += OnToggleAutoPinningConfig;
            ModConfig.Instance.IsSearchWindowEnabledConfig.SettingChanged += OnToggleSearchWindowStartup;
            foreach (ConfigEntry<bool> entry in ModConfig.Instance.TypeDictionary.Keys)
            {
                entry.SettingChanged += OnToggleTypeConfig;
            }
            ToggleAutoPinning();
        }

        private void OnDisable()
        {
            Debug.Log(TextType.MOD_DISABLED);
            PinAssistantScript.Instance?.Destroy();
            if (TrackObjectUI.Instance != null) TrackObjectUI.Instance.enabled = false;
            if (FilterPinsUI.Instance != null) FilterPinsUI.Instance.enabled = false;
            ModConfig.Instance.IsAutoPinningEnabledConfig.SettingChanged -= OnToggleAutoPinningConfig;
            ModConfig.Instance.IsSearchWindowEnabledConfig.SettingChanged -= OnToggleSearchWindowStartup;
            foreach (ConfigEntry<bool> entry in ModConfig.Instance.TypeDictionary.Keys)
            {
                entry.SettingChanged -= OnToggleTypeConfig;
            }
            ToggleAutoPinning();
        }

        private void CheckScene()
        {
            m_isInGame = SceneManager.GetActiveScene().name.Equals("main");
        }

        private void OnSceneChange(Scene scene, LoadSceneMode mode)
        {
            Debug.Log(TextType.SCENE_CHANGE, scene.name);
            CheckScene();
            ModToggle();
        }

        /// <summary>
        /// Toggles the auto-pinning behavior based on the current settings and state.
        /// </summary>
        private void ToggleAutoPinning()
        {
            if (!enabled || !ModConfig.Instance.IsAutoPinningEnabledConfig.Value)
            {
                if (m_coroutineAutoPin == null) return;
                StopCoroutine(m_coroutineAutoPin);
                m_coroutineAutoPin = null;
            }
            else
            {
                if (m_coroutineAutoPin != null) return;
                m_coroutineAutoPin = StartCoroutine("StartAutoPinCoroutine");
            }
        }

        private void LoadTrackObjectUI()
        {
            if (!m_isInGame && !ModConfig.Instance.IsDebugModeConfig.Value) return;
            TrackObjectUI.Init(m_assetBundle);
        }

        private void LoadMinimapFilterUI()
        {
            FilterPinsUI.Init(m_assetBundle, ModConfig.Instance.IsSearchWindowEnabledConfig.Value);
        }

        private void OnTogglePluginConfig(object sender, EventArgs e)
        {
            ModToggle();
        }

        private void OnToggleAutoPinningConfig(object sender, EventArgs e)
        {
            ToggleAutoPinning();
        }

        private void OnToggleSearchWindowStartup(object sender, EventArgs e)
        {
            if (FilterPinsUI.Instance == null) return;
            FilterPinsUI.Instance.ShowOnStartup = ((ConfigEntry<bool>)sender).Value;
        }

        private void OnNewTrackedObject()
        {
            // updates config with new tracked objects;
            string newTrackedObjects = PinAssistantScript.Instance.SerializeTrackedObjects();
            ModConfig.Instance.TrackedObjectsConfig.Value = newTrackedObjects;
        }

        private void OnToggleTypeConfig(object sender, EventArgs _)
        {
            ConfigEntry<bool> currTypeConfig = (ConfigEntry<bool>)sender;
            Type currType = ModConfig.Instance.TypeDictionary[currTypeConfig];
            bool value = currTypeConfig.Value;
            if (value) PinAssistantScript.Instance.AddType(currType);
            else PinAssistantScript.Instance.RemoveType(currType);
        }
    }
}