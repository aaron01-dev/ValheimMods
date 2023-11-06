using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WxAxW.PinAssistant.Components
{
    public class CollapsibleButtonBehavior : MonoBehaviour
    {
        private Button m_button;
        private TMP_Text m_textButton;
        [SerializeField] private GameObject[] m_targetObjs;

        private UnityEvent<bool> m_OnValueChanged = new UnityEvent<bool>();
        public UnityEvent<bool> OnValueChanged { get => m_OnValueChanged; set => m_OnValueChanged = value; }

        [SerializeField] private bool m_IsOn = false;
        public bool IsOn
        {
            get => m_IsOn;
            set
            {
                SetState(value);
            }
        }
        private void Awake()
        {
            m_button = GetComponent<Button>();
            m_textButton = m_button.GetComponentInChildren<TMP_Text>();
            if (m_textButton == null) Debug.LogError("Collapsible button text not found!");
            m_button.onClick.AddListener(() => IsOn = !IsOn);
        }

        private void SetState(bool value)
        {
            if (m_IsOn == value)
                return;

            m_IsOn = value;
            if (value)
            {
                m_textButton.SetText("v");

            }
            else
            {
                m_textButton.SetText(">");
            }

            foreach (GameObject obj in m_targetObjs)
            {
                obj.SetActive(value);
            }
            OnValueChanged.Invoke(value);
        }
    }
}