using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using WxAxW.PinAssistant.Components;
using WxAxW.PinAssistant.Configuration;
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
        public const string PluginVersion = "1.0.1";

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        public static Plugin m_instance;
        public bool m_isInGame = false; // variable to check if in game
        private bool m_isEnabled = false; // variable to determine if the plugin IS actually enabled or not

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
            Components.PinAssistant.Instance.Init(ModConfig.Instance.TrackedObjectsConfig.Value);

            // add to dictionary (change this I guess)
            //m_trackedTypes.Add(m_objectIds, m_objectIds.Value);
            ModConfig.Instance.IsEnabledConfig.SettingChanged += OnTogglePluginConfig;    // add permanent listener to mod toggle
            SceneManager.sceneLoaded += OnSceneChange;  // subscribe regardless if in main menu or in game or whatever
            SceneManager.sceneLoaded += TMPGUIManager.Instance.Init;
            m_assetBundle = AssetUtils.LoadAssetBundleFromResources("pin_assistant");

            CheckScene();   // check current scene
            ModToggle();
            Debug.Log(TextType.PLUGIN_ENABLED);
        }

        private void Update()
        {
            if (m_isEnabled)
            {
                if (ModConfig.Instance.TrackLookedObjectConfig.Value.IsDown())
                {
                    GameObject obj = Components.PinAssistant.Instance.LookAt(ModConfig.Instance.LookDistanceConfig.Value);
                    PinAssistantUI.Instance?.SetupTrackObject(obj);
                }
                if (ModConfig.Instance.PinLookedObjectConfig.Value.IsDown())
                    Components.PinAssistant.Instance.PinLookedObject(ModConfig.Instance.LookDistanceConfig.Value, ModConfig.Instance.RedundancyDistanceConfig.Value);
                if (ModConfig.Instance.ReloadTrackedObjectsConfig.Value.IsDown())
                    //AutoPinning.Instance.TrackLookedObjectToAutoPin(ModConfig.Instance.LookDistanceConfig.Value);
                    Components.PinAssistant.Instance.DeserializeTrackedObjects(ModConfig.Instance.TrackedObjectsConfig.Value);
            }
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            ModDisable();
            // unsubscribe loose listeners
            ModConfig.Instance.IsEnabledConfig.SettingChanged -= OnTogglePluginConfig;    // add permanent listener to mod toggle
            SceneManager.sceneLoaded -= OnSceneChange;  // subscribe regardless if in main menu or in game or whatever

            Debug.Log(TextType.PLUGIN_DISABLED);
        }

        private IEnumerator StartAutoPinCoroutine()
        {
            while (true)
            {
                Components.PinAssistant.Instance.PinLookedObject(ModConfig.Instance.LookDistanceConfig.Value, ModConfig.Instance.RedundancyDistanceConfig.Value);
                yield return new WaitForSeconds(ModConfig.Instance.TickRateConfig.Value);
            }
        }

        private void ModToggle()
        {
            // is the config enabled and you're in game?
            bool valid = ModConfig.Instance.IsEnabledConfig.Value && m_isInGame;
            if (valid && !m_isEnabled) // was the plugin disabled as well? (to avoid reexecuting modenable if the plugin was already enabled)
            {
                ModEnable();
            }
            else if (!valid) // if config and in game is false and plugin was enabled
            {
                ModDisable();
            }
        }

        private void ModEnable()
        {
            Debug.Log(TextType.MOD_ENABLED);
            m_isEnabled = true;
            GUIManager.OnCustomGUIAvailable += LoadUI;
            Components.PinAssistant.Instance.EnableClass();
            Components.PinAssistant.Instance.ModifiedTrackedObjects += OnNewTrackedObject;
            ModConfig.Instance.IsAutoPinningEnabledConfig.SettingChanged += OnToggleAutoPinningConfig;
            ToggleAutoPinning();
        }

        private void ModDisable()
        {
            Debug.Log(TextType.MOD_DISABLED);
            m_isEnabled = false;
            GUIManager.OnCustomGUIAvailable -= LoadUI;
            Components.PinAssistant.Instance.DisableClass();
            Components.PinAssistant.Instance.ModifiedTrackedObjects -= OnNewTrackedObject;
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
            if (!m_isEnabled || !ModConfig.Instance.IsAutoPinningEnabledConfig.Value)
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

        private void LoadUI()
        {
            PinAssistantUI.Init(m_assetBundle);
        }

        private void OnTogglePluginConfig(object sender, EventArgs e)
        {
            ModToggle();
        }

        private void OnToggleAutoPinningConfig(object sender, EventArgs e)
        {
            ToggleAutoPinning();
        }

        private void OnNewTrackedObject()
        {
            // updates config with new tracked objects;
            string newTrackedObjects = Components.PinAssistant.Instance.SerializeTrackedObjects();
            ModConfig.Instance.TrackedObjectsConfig.Value = newTrackedObjects;
        }
    }
}