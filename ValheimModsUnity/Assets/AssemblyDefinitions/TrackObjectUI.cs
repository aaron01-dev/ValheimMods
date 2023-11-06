using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WxAxW.PinAssistant.Components {
    public class TrackObjectUI : MonoBehaviour
    {
        [SerializeField] private Image m_panel;
        [SerializeField] private Transform m_header;
        [SerializeField] private TMP_Text m_headerText;
        [SerializeField] private Image m_body;

        [SerializeField] private Image m_previewIcon;
        [SerializeField] private Image m_previewIconChecked;
        [SerializeField] private TMP_Text m_previewIconText;

        [SerializeField] private TMP_InputField m_inputPinName;
        [SerializeField] private Toggle m_toggleRenamePins;

        [SerializeField] private TMP_InputField m_inputObjectID;
        [SerializeField] private TMP_InputField m_inputBlackListWord;

        [SerializeField] private TMP_Dropdown m_dropDownPinIcon;
        [SerializeField] private Button m_pinColorBox;

        [SerializeField] private TMP_Dropdown m_dropDownTracked;

        [SerializeField] private Toggle m_toggleSavePin;
        [SerializeField] private Toggle m_toggleCheckPin;
        [SerializeField] private Toggle m_toggleExactMatch;

        [SerializeField] private TMP_Text m_messageBox;

        [SerializeField] private Button m_buttonTrackModify;
        [SerializeField] private TMP_Text m_buttonTrackModifyText;
        [SerializeField] private Button m_buttonUntrackCancel;
        [SerializeField] private TMP_Text m_buttonUntrackCancelText;

        [SerializeField] private Transform m_creditRow;
        [SerializeField] private TMP_Text m_versionNumber;
    }

}