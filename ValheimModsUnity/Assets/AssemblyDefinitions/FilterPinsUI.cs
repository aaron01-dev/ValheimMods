using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WxAxW.PinAssistant.Components {
    public class FilterPinsUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_body;
        [SerializeField] private TMP_InputField m_inputPinNameFilter;
        [SerializeField] private Button m_buttonFind;
        [SerializeField] private Button m_buttonReset;
        [SerializeField] private Toggle m_toggleIsWhiteList;
        [SerializeField] private Toggle m_toggleIsRegEx;
    }

}