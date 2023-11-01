using BepInEx;
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
using WxAxW.PinAssistant.Patches;
using WxAxW.PinAssistant.Utils;
using Component = WxAxW.PinAssistant.Core.Component;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.WxAxW" + "." + PluginName;
        public const string PluginName = "PinAssistant";
        public const string PluginVersion = "1.5.1";

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
        private List<Component> pluginComponents;

        private void Awake()
        {
            if (m_instance == null) m_instance = this;

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html
            ModConfig.Init(Config);

            m_assetBundle = AssetUtils.LoadAssetBundleFromResources("pin_assistant");
            ModConfig.Instance.IsEnabledConfig.SettingChanged += OnTogglePluginConfig;    // add permanent listener to mod toggle
            SceneManager.sceneLoaded += OnSceneChange;  // subscribe regardless if in main menu or in game or whatever
            SceneManager.sceneLoaded += GUIManager.Instance.InitialTMPLoad;

            // initialize ui after event loads
            GUIManager.OnCustomGUIAvailable += LoadTrackObjectUI;
            MinimapManager.OnVanillaMapAvailable += LoadMinimapFilterUI;

            pluginComponents = new List<Component>()
            {
                TrackingAssistant.Instance,
                MinimapAssistant.Instance
            };

            foreach (Component comp in pluginComponents)
            {
                comp.Start();
            }
            PatchAll();
            Debug.Log(TextType.PLUGIN_ENABLED);
        }

        private void Update()
        {
            if (ModConfig.Instance.TrackLookedObjectConfig.Value.IsDown())
            {
                TrackingAssistant.Instance.LookAt(ModConfig.Instance.LookDistanceConfig.Value, out string id, out GameObject _);
                TrackObjectUI.Instance?.SetupTrackObject(id);
            }

            if (ModConfig.Instance.PinLookedObjectConfig.Value.IsDown())
                TrackingAssistant.Instance.PinLookedObject(ModConfig.Instance.LookDistanceConfig.Value, ModConfig.Instance.RedundancyDistanceConfig.Value);

            if (ModConfig.Instance.ReloadTrackedObjectsConfig.Value.IsDown())
                //AutoPinning.Instance.TrackLookedObjectToAutoPin(ModConfig.Instance.LookDistanceConfig.Value);
                TrackingAssistant.Instance.DeserializeTrackedObjects(ModConfig.Instance.TrackedObjectsConfig.Value);
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            // unsubscribe loose listeners
            ModConfig.Instance.IsEnabledConfig.SettingChanged -= OnTogglePluginConfig;
            SceneManager.sceneLoaded -= OnSceneChange;  // subscribe regardless if in main menu or in game or whatever
            GUIManager.OnCustomGUIAvailable -= LoadTrackObjectUI;
            MinimapManager.OnVanillaMapAvailable -= LoadMinimapFilterUI;

            foreach (Component comp in pluginComponents)
            {
                comp.Destroy();
            }
            Debug.Log(TextType.PLUGIN_DISABLED);
        }

        private IEnumerator StartAutoPinCoroutine()
        {
            while (true)
            {
                TrackingAssistant.Instance.PinLookedObject(ModConfig.Instance.LookDistanceConfig.Value, ModConfig.Instance.RedundancyDistanceConfig.Value);
                yield return new WaitForSeconds(ModConfig.Instance.TickRateConfig.Value);
            }
        }

        private void PatchAll()
        {
            harmony.PatchAll(typeof(MinimapPatches));
            if (ModExists("Pinnacle"))
            {
                Debug.Log("Pinnacle exists patching mod for compatibility");
                harmony.PatchAll(typeof(PinnaclePatches));
            }
            if (ModExists("Kits_Bitz.Under_The_Radar"))
            {
                Debug.Log("Under_The_Radar exists patching mod for compatibility");
                harmony.PatchAll(typeof(UnderTheRadarPatches));
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

            if (TrackObjectUI.Instance != null) TrackObjectUI.Instance.enabled = true;
            if (FilterPinsUI.Instance != null) FilterPinsUI.Instance.enabled = true;

            ModConfig.Instance.IsAutoPinningEnabledConfig.SettingChanged += OnToggleAutoPinningConfig;

            foreach (Component comp in pluginComponents)
            {
                comp.enabled = true;
            }
            ToggleAutoPinning();
        }

        private void OnDisable()
        {
            Debug.Log(TextType.MOD_DISABLED);
            foreach (Component comp in pluginComponents)
            {
                comp.enabled = false;
            }
            if (TrackObjectUI.Instance != null) TrackObjectUI.Instance.enabled = false;
            if (FilterPinsUI.Instance != null) FilterPinsUI.Instance.enabled = false;
            ModConfig.Instance.IsAutoPinningEnabledConfig.SettingChanged -= OnToggleAutoPinningConfig;

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

        private bool ModExists(string assemblyName)
        {
            try
            {
                System.Reflection.Assembly.Load(assemblyName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void PrintLayerNames()
        {
            if (!ModConfig.Instance.IsDebugModeConfig.Value) return;
            int layerCount = 32; // Unity supports up to 32 layers (0 to 31).

            for (int layer = 0; layer < layerCount; layer++)
            {
                string layerName = LayerMask.LayerToName(layer);
                Debug.Log("Layer " + layer + ": " + layerName);
            }
        }
    }
}