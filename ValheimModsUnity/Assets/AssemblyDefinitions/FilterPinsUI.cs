using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WxAxW.PinAssistant.Components {
    public class FilterPinsUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_body;

        [SerializeField] private CollapsibleButtonBehavior m_buttonReplaceMode;

        [SerializeField] private TMP_InputField m_inputPinNameFind;
        [SerializeField] private TMP_Dropdown m_dropdownPinIconFind;

        [SerializeField] private TMP_InputField m_inputPinNameReplace;
        [SerializeField] private TMP_Dropdown m_dropdownPinIconReplace;

        [SerializeField] private Toggle m_toggleIsWhiteList;

        [SerializeField] private Button m_buttonFind;
        [SerializeField] private Button m_buttonReset;
        [SerializeField] private Button m_buttonReplace;

        [SerializeField] private Toggle m_toggleIsRegEx;

    }
}