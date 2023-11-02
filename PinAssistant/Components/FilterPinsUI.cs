using BepInEx.Configuration;
using Jotunn.Managers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private bool m_isFocused = false;

#pragma warning disable CS0649
        [SerializeField] private GameObject m_body;
        [SerializeField] private TMP_InputField m_inputPinNameFilter;
        [SerializeField] private Button m_buttonFind;
        [SerializeField] private Button m_buttonReset;
        [SerializeField] private Toggle m_toggleIsWhiteList;
        [SerializeField] private Toggle m_toggleIsRegEx;
#pragma warning restore CS0649

        public bool ShowOnStartup { get => m_showOnStartup; set => m_showOnStartup = value; }
        public GameObject Body { get => m_body; set => m_body = value; }
        public TMP_InputField InputPinNameFilter { get => m_inputPinNameFilter; set => m_inputPinNameFilter = value; }
        public Button ButtonFind { get => m_buttonFind; set => m_buttonFind = value; }
        public Button ButtonReset { get => m_buttonReset; set => m_buttonReset = value; }
        public Toggle ToggleIsRegEx { get => m_toggleIsRegEx; set => m_toggleIsRegEx = value; }
        public Toggle ToggleIsWhiteList { get => m_toggleIsWhiteList; set => m_toggleIsWhiteList = value; }

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

        private void Start()
        {
            ModConfig.Instance.IsSearchWindowEnabledConfig.SettingChanged += OnToggleSearchWindowStartup; // add a permanent listener to startup config change
        }

        private void Update()
        {
            bool blockInput = Chat.instance != null && Chat.instance.HasFocus() || Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible() || InventoryGui.IsVisible();

            if (blockInput) return;
            if (ModConfig.Instance.ToggleFilterWindowConfig.Value.IsDown())
            {
                m_body.SetActive(!m_body.activeSelf);
                if (m_body.activeSelf) return;
                m_inputPinNameFilter.DeactivateInputField();
                SetFocused(false);
            }
        }

        private void OnDestroy()
        {
            ModConfig.Instance.IsSearchWindowEnabledConfig.SettingChanged -= OnToggleSearchWindowStartup; // remove permanent listener to startup config change
            m_instance = null;
        }

        private void OnEnable()
        {
            m_inputPinNameFilter.onSelect.AddListener(OnInputFocus);
            m_inputPinNameFilter.onDeselect.AddListener(OnInputLossFocus);
            m_inputPinNameFilter.onSubmit.AddListener(OnSubmit);
            m_buttonFind.onClick.AddListener(OnButtonFind);
            m_buttonReset.onClick.AddListener(OnButtonReset);
        }

        private void OnDisable()
        {
            m_inputPinNameFilter.onSelect.RemoveListener(OnInputFocus);
            m_inputPinNameFilter.onDeselect.RemoveListener(OnInputLossFocus);
            m_inputPinNameFilter.onSubmit.RemoveListener(OnSubmit);
            m_buttonFind.onClick.RemoveListener(OnButtonFind);
            m_buttonReset.onClick.RemoveListener(OnButtonReset);
        }

        public void ModEnable()
        {
            m_body.SetActive(m_showOnStartup);
        }

        public void ModDisable()
        {
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

            foreach (Toggle toggle in new Toggle[] { m_toggleIsRegEx, m_toggleIsWhiteList})
            {
                GUIManager.Instance.ApplyTMPToggleStyle(toggle, 14);
            }
        }

        private void OnButtonFind()
        {
            string pinToFind = m_inputPinNameFilter.text.ToLower();
            MinimapAssistant.Instance.SearchPins(pinToFind, m_toggleIsWhiteList.isOn, m_toggleIsRegEx.isOn);
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

        private void OnInputFocus(string _)
        {
            SetFocused(true);
        }

        private void OnInputLossFocus(string _)
        {
            SetFocused(false);
        }

        private void OnSubmit(string _)
        {
            OnButtonFind();
            m_inputPinNameFilter.ActivateInputField();
        }

        private void SetFocused(bool focused)
        {
            if (m_isFocused == focused) return;
            m_isFocused = focused;
            GUIManager.BlockInput(focused);
        }
    }
}