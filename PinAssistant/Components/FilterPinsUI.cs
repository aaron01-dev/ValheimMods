using BepInEx.Configuration;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool m_isFocused = false;
        private List<Minimap.PinType> m_listFindPins = new List<Minimap.PinType>();
        private List<Minimap.PinType> m_listReplacePins = new List<Minimap.PinType>();
        private Minimap.PinType m_findPinType;
        private Minimap.PinType m_replacePinType;

#pragma warning disable CS0649
        [SerializeField] private GameObject m_body;

        [SerializeField] private CollapsibleButtonBehavior m_buttonReplaceMode;

        [SerializeField] private TMP_InputField m_inputPinNameFind;
        [SerializeField] private TMP_Dropdown m_dropdownPinIconFind;

        [SerializeField] private TMP_InputField m_inputPinNameReplace;
        [SerializeField] private TMP_Dropdown m_dropdownPinIconReplace;

        [SerializeField] private Toggle m_toggleIsWhiteList;
        [SerializeField] private Toggle m_toggleIsRegEx;

        [SerializeField] private Button m_buttonFind;
        [SerializeField] private Button m_buttonReset;

        [SerializeField] private Button m_buttonReplace;
#pragma warning restore CS0649

        public bool ShowOnStartup { get => m_showOnStartup; set => m_showOnStartup = value; }
        public GameObject Body { get => m_body; set => m_body = value; }
        public CollapsibleButtonBehavior ButtonReplaceMode { get => m_buttonReplaceMode; set => m_buttonReplaceMode = value; }

        public TMP_InputField InputPinNameFilter { get => m_inputPinNameFind; set => m_inputPinNameFind = value; }
        public TMP_Dropdown DropdownPinIconFind { get => m_dropdownPinIconFind; set => m_dropdownPinIconFind = value; }

        public TMP_InputField InputPinNameReplace { get => m_inputPinNameReplace; set => m_inputPinNameReplace = value; }
        public TMP_Dropdown DropdownPinIconReplace { get => m_dropdownPinIconReplace; set => m_dropdownPinIconReplace = value; }

        public Toggle ToggleIsWhiteList { get => m_toggleIsWhiteList; set => m_toggleIsWhiteList = value; }

        public Button ButtonFind { get => m_buttonFind; set => m_buttonFind = value; }
        public Button ButtonReset { get => m_buttonReset; set => m_buttonReset = value; }
        public Button ButtonReplace { get => m_buttonReplace; set => m_buttonReplace = value; }

        public Toggle ToggleIsRegEx { get => m_toggleIsRegEx; set => m_toggleIsRegEx = value; }
        public bool IsFocused { get => m_isFocused; set => m_isFocused = value; }

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
            if (MinimapAssistant.Instance.DictionaryPinTypePopulated) PopulateDropdownPinType();
            else MinimapAssistant.Instance.OnDictionaryPinTypePopulated += PopulateDropdownPinType;
            ApplyStyle();
            m_body.SetActive(m_showOnStartup);
        }

        private void Start()
        {
            m_buttonReplaceMode.IsOn = false;
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
                m_inputPinNameFind.DeactivateInputField();
                SetFocused(false);
            }
        }

        private void OnDestroy()
        {
            if (ModConfig.Instance != null) ModConfig.Instance.IsSearchWindowEnabledConfig.SettingChanged -= OnToggleSearchWindowStartup; // remove permanent listener to startup config change
            m_instance = null;
        }

        private void OnEnable()
        {
            m_inputPinNameFind.onSelect.AddListener(OnInputFocus);
            m_inputPinNameFind.onDeselect.AddListener(OnInputLossFocus);
            m_inputPinNameReplace.onSelect.AddListener(OnInputFocus);
            m_inputPinNameReplace.onDeselect.AddListener(OnInputLossFocus);
            m_inputPinNameFind.onSubmit.AddListener(OnSubmit);
            m_buttonFind.onClick.AddListener(OnButtonFind);
            m_buttonReset.onClick.AddListener(OnButtonReset);

            m_dropdownPinIconFind.onValueChanged.AddListener(OnDropdownFindChanged);
            m_dropdownPinIconReplace.onValueChanged.AddListener(OnDropdownReplaceChanged);
            m_buttonReplaceMode.OnValueChanged.AddListener(OnToggleReplaceModeClicked);
            m_buttonReplace.onClick.AddListener(OnButtonReplace);
            m_body.SetActive(m_showOnStartup);
        }

        private void OnDisable()
        {
            m_inputPinNameFind.onSelect.RemoveListener(OnInputFocus);
            m_inputPinNameFind.onDeselect.RemoveListener(OnInputLossFocus);
            m_inputPinNameReplace.onSelect.RemoveListener(OnInputFocus);
            m_inputPinNameReplace.onDeselect.RemoveListener(OnInputLossFocus);
            m_inputPinNameFind.onSubmit.RemoveListener(OnSubmit);
            m_buttonFind.onClick.RemoveListener(OnButtonFind);
            m_buttonReset.onClick.RemoveListener(OnButtonReset);

            m_dropdownPinIconFind.onValueChanged.RemoveListener(OnDropdownFindChanged);
            m_dropdownPinIconReplace.onValueChanged.RemoveListener(OnDropdownReplaceChanged);
            m_buttonReplaceMode.OnValueChanged.RemoveListener(OnToggleReplaceModeClicked);
            m_buttonReplace.onClick.RemoveListener(OnButtonReplace);
            m_body.SetActive(false);
        }

        private void ApplyStyle()
        {
            Image m_panel = m_body.GetComponent<Image>();
            m_panel.color = Color.white;
            GUIManager.Instance.ApplyWoodpanelStyle(m_panel.transform);
            // m_panel.GetComponent<Image>().material = null;
            foreach (TMP_InputField input in new TMP_InputField[] { m_inputPinNameFind, m_inputPinNameReplace})
            {
                GUIManager.Instance.ApplyTMPInputFieldStyle(input, 16);
            }

            foreach (TMP_Dropdown dropdown in new TMP_Dropdown[] { m_dropdownPinIconFind, m_dropdownPinIconReplace})
            {
                GUIManager.Instance.ApplyTMPDropdownStyle(dropdown, 16);
            }

            foreach (Button button in new Button[] { m_buttonFind, m_buttonReset, m_buttonReplace, m_buttonReplaceMode.GetComponent<Button>()})
            {
                GUIManager.Instance.ApplyTMPButtonStyle(button, 20);
            }

            foreach (Toggle toggle in new Toggle[] { m_toggleIsRegEx, m_toggleIsWhiteList})
            {
                GUIManager.Instance.ApplyTMPToggleStyle(toggle, 14);
            }
        }

        private void PopulateDropdownPinType()
        {
            if (Minimap.instance == null) return;
            m_dropdownPinIconFind.AddOptionWithList("All", m_listFindPins, Minimap.PinType.None);
            m_dropdownPinIconReplace.AddOptionWithList("Unchanged", m_listReplacePins, Minimap.PinType.None);
            foreach (var kvp in MinimapAssistant.Instance.DictionaryPinType.ToList())
            {
                if (kvp.Key == Minimap.PinType.None) continue;
                Minimap.PinType pinType = kvp.Key;
                string iconName = kvp.Value.Item2;
                m_dropdownPinIconFind.AddOptionWithList(iconName, m_listFindPins, pinType);
                m_dropdownPinIconReplace.AddOptionWithList(iconName, m_listReplacePins, pinType);
            }
            m_findPinType = m_listFindPins[0];
            m_replacePinType = m_listReplacePins[0];
            MinimapAssistant.Instance.OnDictionaryPinTypePopulated -= PopulateDropdownPinType;
        }

        private void OnToggleSearchWindowStartup(object sender, EventArgs eventArgs)
        {
            ShowOnStartup = ((ConfigEntry<bool>)sender).Value;
        }

        private void OnToggleReplaceModeClicked(bool value)
        {
            if (value) m_toggleIsWhiteList.isOn = true; // hardlock to true
            m_toggleIsWhiteList.interactable = !value;
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
            m_inputPinNameFind.ActivateInputField();
        }

        public void OnDropdownFindChanged(int value)
        {
            m_findPinType = m_listFindPins[value];
        }

        public void OnDropdownReplaceChanged(int value)
        {
            m_replacePinType = m_listReplacePins[value];
        }

        private void OnButtonFind()
        {
            MinimapAssistant.Instance.SearchPins(m_inputPinNameFind.text, m_findPinType, m_toggleIsWhiteList.isOn, m_toggleIsRegEx.isOn);
        }

        private void OnButtonReset()
        {
            MinimapAssistant.Instance.ResetFilteredPins();
        }
        private void OnButtonReplace()
        {
            MinimapAssistant.Instance.ModifyPins(m_inputPinNameFind.text, m_inputPinNameReplace.text, m_findPinType, m_replacePinType, m_toggleIsRegEx.isOn);
        }

        private void SetFocused(bool focused)
        {
            if (m_isFocused == focused) return;
            m_isFocused = focused;
            GUIManager.BlockInput(focused);
        }
    }
}