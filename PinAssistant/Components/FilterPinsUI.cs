using BepInEx.Configuration;
using Jotunn.Managers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Core;
using WxAxW.PinAssistant.Utils;

namespace WxAxW.PinAssistant.Components
{
    internal class FilterPinsUI : MonoBehaviour
    {
        private static FilterPinsUI m_instance;
        public static FilterPinsUI Instance => m_instance;
        private bool m_showOnStartup;

#pragma warning disable CS0649
        [SerializeField] private GameObject m_body;
        [SerializeField] private TMP_InputField m_inputPinNameFilter;
        [SerializeField] private Button m_buttonFind;
        [SerializeField] private Button m_buttonReset;
#pragma warning restore CS0649

        public bool ShowOnStartup { get => m_showOnStartup; set => m_showOnStartup = value; }
        public GameObject Body { get => m_body; set => m_body = value; }
        public TMP_InputField InputPinNameFilter { get => m_inputPinNameFilter; set => m_inputPinNameFilter = value; }
        public Button ButtonFind { get => m_buttonFind; set => m_buttonFind = value; }
        public Button ButtonReset { get => m_buttonReset; set => m_buttonReset = value; }

        public static void Init(AssetBundle assetBundle, bool showOnStartup)
        {
            GameObject prefabFilterUI = assetBundle.LoadAsset<GameObject>("Assets/ObjFilterPinsUI.prefab");
            GameObject prefabLayouUI = assetBundle.LoadAsset<GameObject>("Assets/TopLeftGroup.prefab");
            Transform layoutTrans = Instantiate(prefabLayouUI, Minimap.instance.m_largeRoot.transform, false).transform;
            m_instance = Instantiate(prefabFilterUI, layoutTrans, false).GetComponent<FilterPinsUI>();
            m_instance.m_showOnStartup = showOnStartup;
        }

        private void Awake()
        {

            ApplyStyle();
            m_body.SetActive(m_showOnStartup);
        }

        private void Update()
        {
            if (ModConfig.Instance.ToggleFilterWindowConfig.Value.IsDown())
                m_body.SetActive(!m_body.activeSelf);
        }

        private void OnDestroy()
        {
            m_instance = null;
        }

        private void OnEnable()
        {
            ModConfig.Instance.IsSearchWindowEnabledConfig.SettingChanged += OnToggleSearchWindowStartup;
            m_buttonFind.onClick.AddListener(OnButtonFind);
            m_buttonReset.onClick.AddListener(OnButtonReset);
            m_body.SetActive(m_showOnStartup);
        }

        private void OnDisable()
        {
            ModConfig.Instance.IsSearchWindowEnabledConfig.SettingChanged -= OnToggleSearchWindowStartup;
            m_buttonFind.onClick.RemoveListener(OnButtonFind);
            m_buttonReset.onClick.RemoveListener(OnButtonReset);
            m_body.SetActive(false);
        }

        private void ApplyStyle()
        {
            Image m_panel = m_body.GetComponent<Image>();
            m_panel.color = Color.white;
            GUIManager.Instance.ApplyWoodpanelStyle(m_panel.transform);

            GUIManager.Instance.ApplyTMPInputFieldStyle(m_inputPinNameFilter, 16);

            foreach (Button button in new Button[] { m_buttonFind, m_buttonReset })
            {
                GUIManager.Instance.ApplyTMPButtonStyle(button, 20);
            }
        }

        private void OnButtonFind()
        {
            string pinToFind = m_inputPinNameFilter.text.ToLower();
            MinimapAssistant.Instance.SearchPins(pinToFind);
        }

        private void OnButtonReset()
        {
            m_inputPinNameFilter.text = "";
            MinimapAssistant.Instance.ResetFilteredPins();
        }

        private void OnToggleSearchWindowStartup(object sender, EventArgs eventArgs)
        {
            ShowOnStartup = ((ConfigEntry<bool>)sender).Value;
        }
    }
}