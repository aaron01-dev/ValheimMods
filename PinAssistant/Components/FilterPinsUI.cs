using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Utils;

namespace WxAxW.PinAssistant.Components
{
    internal class FilterPinsUI : MonoBehaviour
    {
        private static FilterPinsUI m_instance;
        public static FilterPinsUI Instance => m_instance;
        private bool m_showOnStartup;

        public bool ShowOnStartup { get => m_showOnStartup; set => m_showOnStartup = value; }

#pragma warning disable CS0649
        [SerializeField] private GameObject m_body;
        [SerializeField] private TMP_InputField m_inputPinNameFilter;
        [SerializeField] private Button m_buttonFind;
        [SerializeField] private Button m_buttonReset;
#pragma warning restore CS0649

        private static IEnumerable<Minimap.PinData> m_listFilteredPins;

        public static void Init(AssetBundle assetBundle, bool showOnStartup)
        {
            GameObject prefabFilterUI = assetBundle.LoadAsset<GameObject>("Assets/ObjFilterPinsUI.prefab");
            GameObject prefabLayouUI = assetBundle.LoadAsset<GameObject>("Assets/TopLeftGroup.prefab");
            Transform layoutTrans = Instantiate(prefabLayouUI, Minimap.m_instance.m_largeRoot.transform, false).transform;
            m_instance = Instantiate(prefabFilterUI, layoutTrans, false).GetComponent<FilterPinsUI>();
            m_instance.m_showOnStartup = showOnStartup;
        }

        private void Awake()
        {
            m_buttonFind.onClick.AddListener(OnButtonFind);
            m_buttonReset.onClick.AddListener(ResetPins);
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
            m_buttonFind.onClick.RemoveListener(OnButtonFind);
            m_buttonReset.onClick.RemoveListener(ResetPins);
            m_instance = null;
        }

        private void OnEnable()
        {
            m_body.SetActive(m_showOnStartup);
        }

        private void OnDisable()
        {
            m_inputPinNameFilter.text = "";
            ResetPins();
            m_body.SetActive(false);
        }

        private void ApplyStyle()
        {
            Image m_panel = m_body.GetComponent<Image>();
            m_panel.color = Color.white;
            TMPGUIManager.Instance.ApplyWoodpanel(m_panel);

            TMPGUIManager.Instance.ApplyInputFieldStyle(m_inputPinNameFilter, 16);

            foreach (Button button in new Button[] { m_buttonFind, m_buttonReset })
            {
                TMPGUIManager.Instance.ApplyButtonStyle(button, 20);
            }
        }

        private void OnButtonFind()
        {
            ResetPins();

            string pinToFind = m_inputPinNameFilter.text.ToLower();
            // Define a regular expression pattern to match strings with double quotes at the front and back
            string pattern = "^\".*\"$";

            // Use Regex.IsMatch to check if the input matches the pattern
            bool isExact = Regex.IsMatch(pinToFind, pattern) || string.IsNullOrEmpty(pinToFind);
            if (isExact) pinToFind = pinToFind.Trim('"');

            m_listFilteredPins = Minimap.m_instance.m_pins
                .Where(pinData => !CompareSearch(pinData.m_name, pinToFind, isExact));
        }

        private void ResetPins()
        {
            if (m_listFilteredPins == null) return;
            FilterPins(renderPins: true);
            m_listFilteredPins = null;
        }

        public void FilterPins()
        {
            if (m_listFilteredPins == null) return;
            FilterPins(renderPins: false);
        }

        public void FilterPins(bool renderPins)
        {
            foreach (var pin in m_listFilteredPins)
            {
                pin.m_NamePinData?.PinNameGameObject?.SetActive(renderPins);
                pin.m_uiElement?.gameObject.SetActive(renderPins);
            }
        }

        private bool CompareSearch(string foundPin, string query, bool isExact)
        {
            foundPin = foundPin.ToLower();
            if (isExact) return foundPin.Equals(query);

            return foundPin.IndexOf(query) != -1;
        }
    }
}