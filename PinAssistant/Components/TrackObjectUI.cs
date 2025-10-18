using Jotunn.GUI;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Core;
using WxAxW.PinAssistant.Utils;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Components
{
    internal class TrackObjectUI : MonoBehaviour
    {
        private static TrackObjectUI m_instance;
        public static TrackObjectUI Instance => m_instance;

        private readonly string[] m_modifiableText = new string[] {
            TextAttribute.Get(TextType.HEADER_TRACK),
            TextAttribute.Get(TextType.HEADER_MODIFY),
            TextAttribute.Get(TextType.BUTTON_TRACK),
            TextAttribute.Get(TextType.BUTTON_MODIFY),
            TextAttribute.Get(TextType.BUTTON_CANCEL),
            TextAttribute.Get(TextType.BUTTON_UNTRACK)
        };

#pragma warning disable CS0649
        [SerializeField] private Image m_panel;
        [SerializeField] private Transform m_header;
        [SerializeField] private TMP_Text m_headerText;
        [SerializeField] private Image m_body;

        [SerializeField] private Image m_previewIcon;
        [SerializeField] private Image m_previewIconChecked;
        [SerializeField] private TMP_Text m_previewIconText;

        [SerializeField] private TMP_InputField m_inputPinName;
        [SerializeField] private Toggle m_toggleModifyPins;

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
#pragma warning restore CS0649

        private Color m_pinColor;
        private bool m_editMode = false;
        private string m_oldObjectID = string.Empty; // if creating a new entry, this is for autofill area, used by "exactMatch" toggle to lock the user from editing inputObjectID field
        private TrackedObject m_edittingObject; // when editing an entry, used to have a data to compare values to the new values
        private bool m_edittingColor = false;

        private readonly List<TrackedObject> m_dropDownTrackedList = new List<TrackedObject>(); // drop down object equivalent to dropdown's values
        private Minimap.PinType m_pinTypeInput;

        public static event Action TrackObjectUILoaded;

        public Image Panel { get => m_panel; set => m_panel = value; }
        public Transform Header { get => m_header; set => m_header = value; }
        public TMP_Text HeaderText { get => m_headerText; set => m_headerText = value; }
        public Image Body { get => m_body; set => m_body = value; }

        public Image PreviewIcon { get => m_previewIcon; set => m_previewIcon = value; }
        public Image PreviewIconChecked { get => m_previewIconChecked; set => m_previewIconChecked = value; }
        public TMP_Text PreviewIconText { get => m_previewIconText; set => m_previewIconText = value; }

        public TMP_InputField InputPinName { get => m_inputPinName; set => m_inputPinName = value; }
        public Toggle ToggleModifyPins { get => m_toggleModifyPins; set => m_toggleModifyPins = value; }

        public TMP_InputField InputObjectID { get => m_inputObjectID; set => m_inputObjectID = value; }
        public TMP_InputField InputBlackListWord { get => m_inputBlackListWord; set => m_inputBlackListWord = value; }

        public TMP_Dropdown DropDownPinIcon { get => m_dropDownPinIcon; set => m_dropDownPinIcon = value; }
        public Button PinColor { get => m_pinColorBox; set => m_pinColorBox = value; }

        public TMP_Dropdown DropDownTracked { get => m_dropDownTracked; set => m_dropDownTracked = value; }

        public Toggle ToggleSavePin { get => m_toggleSavePin; set => m_toggleSavePin = value; }
        public Toggle ToggleCheckPin { get => m_toggleCheckPin; set => m_toggleCheckPin = value; }
        public Toggle ToggleExactMatch { get => m_toggleExactMatch; set => m_toggleExactMatch = value; }

        public TMP_Text MessageBox { get => m_messageBox; set => m_messageBox = value; }

        public Button ButtonTrackModify { get => m_buttonTrackModify; set => m_buttonTrackModify = value; }
        public TMP_Text ButtonTrackModifyText { get => m_buttonTrackModifyText; set => m_buttonTrackModifyText = value; }
        public Button ButtonUntrackCancel { get => m_buttonUntrackCancel; set => m_buttonUntrackCancel = value; }
        public TMP_Text ButtonUntrackCancelText { get => m_buttonUntrackCancelText; set => m_buttonUntrackCancelText = value; }

        public Transform CreditRow { get => m_creditRow; set => m_creditRow = value; }
        public TMP_Text VersionNumber { get => m_versionNumber; set => m_versionNumber = value; }

        public static void Init(AssetBundle assetBundle)
        {
            GameObject prefab = assetBundle.LoadAsset<GameObject>("Assets/ObjTrackObjectUI.prefab");
            m_instance = Instantiate(prefab, GUIManager.CustomGUIFront.transform, false).GetComponent<TrackObjectUI>();
        }

#pragma warning disable IDE0051 // Remove unused private members

        private void Awake()
#pragma warning restore IDE0051 // Remove unused private members
        {
            if (PinHandler.Instance.DictionaryPinTypePopulated) PopulateDropdownPinType();
            else PinHandler.Instance.OnDictionaryPinTypePopulated += PopulateDropdownPinType;
            PopulateDropdownTracked(TrackingHandler.Instance.TrackedObjects);
            ApplyStyle();
            m_versionNumber.text = $"v{Plugin.PluginVersion}";
            m_previewIconChecked.sprite = GUIManager.Instance.GetSprite("mapicon_checked");
            m_previewIconText.text = string.Empty; // reset
            m_previewIconChecked.gameObject.SetActive(false); // reset
            m_toggleModifyPins.gameObject.SetActive(false);

            m_inputPinName.onValueChanged.AddListener(OnPinNameChange);
            m_buttonTrackModify.onClick.AddListener(OnButtonTrackedModifyPressed);
            m_buttonUntrackCancel.onClick.AddListener(OnButtonUntrackedCancelPressed);
            m_dropDownPinIcon.onValueChanged.AddListener(OnPinIconDropDownChanged);
            m_pinColorBox.onClick.AddListener(ShowColorPicker);
            m_dropDownTracked.onValueChanged.AddListener(OnTrackedDropDownChanged);
            m_toggleCheckPin.onValueChanged.AddListener(OnToggleCheckPinChanged);
            m_toggleExactMatch.onValueChanged.AddListener(OnToggleExactMatchChanged);
            TrackingHandler.Instance.OnTrackedObjectsReload += PopulateDropdownTracked;
            TrackingHandler.Instance.OnTrackedObjectSaved += PopulateDropdownTracked;

            TrackObjectUILoaded?.Invoke();
            //m_buttonUntrackCancel.onClick.AddListener()
            enabled = false;
        }

        private void Update()
        {
            // keybind to close ui
            if (Input.GetKeyDown(KeyCode.Escape))
                enabled = false;
            if (Input.GetKeyDown(KeyCode.Return))
                OnButtonTrackedModifyPressed();
        }

        private void OnDestroy()
        {
            m_inputPinName.onValueChanged.RemoveListener(OnPinNameChange);
            m_buttonTrackModify.onClick.RemoveListener(OnButtonTrackedModifyPressed);
            m_buttonUntrackCancel.onClick.RemoveListener(OnButtonUntrackedCancelPressed);
            m_dropDownPinIcon.onValueChanged.RemoveListener(OnPinIconDropDownChanged);
            m_pinColorBox.onClick.RemoveListener(ShowColorPicker);
            m_dropDownTracked.onValueChanged.RemoveListener(OnTrackedDropDownChanged);
            m_toggleCheckPin.onValueChanged.RemoveListener(OnToggleCheckPinChanged);
            m_toggleExactMatch.onValueChanged.RemoveListener(OnToggleExactMatchChanged);
            if (TrackingHandler.Instance != null)
            {
                TrackingHandler.Instance.OnTrackedObjectsReload -= PopulateDropdownTracked;
                TrackingHandler.Instance.OnTrackedObjectSaved -= PopulateDropdownTracked;
            }
            m_instance = null;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
            SetUIActive(false);
        }

        private void ApplyStyle()
        {
            m_panel.color = Color.white;
            GUIManager.Instance.ApplyWoodpanelStyle(m_panel.transform);
            // m_panel.GetComponent<Image>().material = null;

            GUIManager.Instance.ApplyTMPTextStyle(m_header.GetChild(0).GetComponent<TMP_Text>(), GUIManagerExtension.TMPNorse, GUIManager.Instance.ValheimOrange, 36, true);

            // apply text to everything as a pre setup for objects not referenced
            foreach (TMP_Text text in m_body.GetComponentsInChildren<TMP_Text>())
            {
                GUIManager.Instance.ApplyTMPTextStyle(text, GUIManager.Instance.ValheimOrange, 20, true);
            }

            foreach (TMP_Text text in m_creditRow.GetComponentsInChildren<TMP_Text>())
            {
                GUIManager.Instance.ApplyTMPTextStyle(text, GUIManager.Instance.ValheimYellow, 12, true);
            }

            // change preview icon name style
            GUIManager.Instance.ApplyTMPTextStyle(m_previewIconText, GUIManagerExtension.TMPNorse, Color.white, 16);

            foreach (TMP_InputField inputField in new TMP_InputField[] { m_inputPinName, m_inputObjectID, m_inputBlackListWord })
            {
                GUIManager.Instance.ApplyTMPInputFieldStyle(inputField, 16);
            }

            foreach (Toggle toggle in new Toggle[] { m_toggleSavePin, m_toggleCheckPin, m_toggleExactMatch })
            {
                GUIManager.Instance.ApplyTMPToggleStyle(toggle, 16);
            }
            GUIManager.Instance.ApplyTMPToggleStyle(m_toggleModifyPins, 14);

            foreach (Button button in new Button[] { m_buttonTrackModify, m_buttonUntrackCancel })
            {
                GUIManager.Instance.ApplyTMPButtonStyle(button, 20);
            }

            GUIManager.Instance.ApplyTMPDropdownStyle(m_dropDownPinIcon);
            GUIManager.Instance.ApplyTMPDropdownStyle(m_dropDownTracked);
            GUIManager.Instance.ApplyTMPTextStyle(m_messageBox, GUIManager.Instance.ValheimOrange, 16);
            m_messageBox.textWrappingMode = TextWrappingModes.Normal;
        }

        private void PopulateDropdownPinType()
        {
            if (Minimap.instance == null) return;
            foreach (var kvp in PinHandler.Instance.DictionaryPinType.ToList())
            {
                if (kvp.Key == Minimap.PinType.None) continue;
                string iconName = kvp.Value.Item2;
                m_dropDownPinIcon.options.Add(new TMP_Dropdown.OptionData(iconName));
            }
            PinHandler.Instance.OnDictionaryPinTypePopulated -= PopulateDropdownPinType;
        }

        private void PopulateDropdownTracked(LooseDictionary<TrackedObject> trackedObjects)
        {
            m_dropDownTracked.ClearOptions();
            m_dropDownTrackedList.Clear();
            m_dropDownTracked.options.Add(new TMP_Dropdown.OptionData("New..."));
            m_dropDownTrackedList.Add(default);

            foreach (LooseDictionary<TrackedObject>.TrieNode node in trackedObjects.AltDictionary.Values)
            {
                TrackedObject trackedObject = node.Value;
                m_dropDownTrackedList.Add(trackedObject);
                m_dropDownTracked.options.Add(new TMP_Dropdown.OptionData(trackedObject.Name));
            }
            m_dropDownTracked.RefreshShownValue();
            Debug.Log(TextType.OBJECTS_DROPDOWN_LOADED);
        }

        private void BlockInput(bool value)
        {
            value = !value;

            Selectable[] inputs = new Selectable[] {
                m_inputPinName,
                m_inputObjectID,
                m_inputBlackListWord,
                m_dropDownPinIcon,
                m_pinColorBox,
                m_toggleSavePin,
                m_toggleCheckPin,
                m_toggleExactMatch,
                m_dropDownTracked,
                m_buttonTrackModify,
                m_buttonUntrackCancel
            };

            foreach (Selectable input in inputs)
            {
                input.interactable = value;
            }

            OnToggleExactMatchChanged(m_toggleExactMatch.isOn);
        }

        public void SetUIActive(bool value)
        {
            GUIManager.BlockInput(value);
            gameObject.SetActive(value);
            if (m_edittingColor) ColorPicker.Cancel();
        }

        public void SetupTrackObject(string objName)
        {
            enabled = !enabled;
            SetUIActive(enabled);
            // check if opening or closing
            if (!enabled) return;

            // setup values
            m_dropDownTracked.SetValueWithoutNotify(0);
            m_messageBox.text = string.Empty;
            m_edittingObject = null;
            SetupUIValues(objName);
        }

        public void SetupUIValues(string _objectID, bool _exactMatch = false)
        {
            // reset values when changing to a different entry
            m_oldObjectID = string.Empty;
            m_toggleModifyPins.isOn = false;

            // check if object id loosely matches a key in dictionary
            string formattedName = string.Empty;
            string objIDName = _objectID;
            string blackListWords = "";
            Color color = Color.white;
            bool isSaved = true, isChecked = false, IsExactMatchOnly = false;
            TrackedObject trackedObject = null;
            int pinType = (int)Minimap.PinType.Icon3;

            if (!string.IsNullOrEmpty(objIDName))
            {
                if (TrackingHandler.Instance.TrackedObjects.TryGetValueLoose(objIDName, out trackedObject, _exactMatch))
                {
                    formattedName = trackedObject.Name;
                    objIDName = trackedObject.ObjectID;
                    blackListWords = trackedObject.BlackListWords;
                    pinType = trackedObject.GetPinIntAsDropdown();
                    color = trackedObject.PinColor;
                    isSaved = trackedObject.Save;
                    isChecked = trackedObject.IsChecked;
                    IsExactMatchOnly = trackedObject.IsExactMatchOnly;
                    m_dropDownTracked.SetValueWithoutNotify(m_dropDownTrackedList.IndexOf(trackedObject)); // change dropdown input
                    if (IsExactMatchOnly) m_oldObjectID = objIDName; // to allow the user to modify the object id when they created an entry that's not "exact match"
                }
                else
                {
                    formattedName = TrackingHandler.Instance.FormatObjectName(objIDName);
                    m_oldObjectID = objIDName; // to allow the user to modify the object id when they created an entry that's not "exact match"
                    IsExactMatchOnly = true;
                }
                // m_previewIcon.sprite = AutoPinning.Instance.TrackedObjects[newValue].Icon;
            }

            m_inputPinName.text = formattedName;
            m_inputObjectID.text = objIDName;
            m_inputBlackListWord.text = blackListWords;
            m_dropDownPinIcon.value = pinType;
            SelectColor(color);

            m_toggleSavePin.isOn = !isSaved;
            m_toggleCheckPin.isOn = isChecked;
            m_toggleExactMatch.isOn = IsExactMatchOnly;
            ChangeUIMode(trackedObject);
        }

        private void ChangeUIMode(TrackedObject trackedObject = null)
        {
            m_editMode = false;
            if (trackedObject != null)
            {
                m_editMode = true;
                m_edittingObject = trackedObject;
                int newDropdownIndex = m_dropDownTrackedList.IndexOf(trackedObject);
                m_dropDownTracked.SetValueWithoutNotify(newDropdownIndex); // switch to specified drop down
            }
            m_toggleModifyPins.gameObject.SetActive(m_editMode);
            int index = !m_editMode ? 0 : 1;
            m_headerText.text = m_modifiableText[index];

            m_buttonTrackModifyText.text = m_modifiableText[index + 2];
            m_buttonUntrackCancelText.text = m_modifiableText[index + 4];
        }

        private void TrackNewObject()
        {
            Debug.Log($"Attempting to add {m_inputObjectID.text}");
            TrackedObject trackedObject;
            if (string.IsNullOrEmpty(m_inputObjectID.text))
            {
                ShowMessage(Debug.Log(TextType.TRACK_INVALID));
                return;
            }

            // double check if the objectID already exists
            if (TrackingHandler.Instance.TrackedObjects.TryGetValueLoose(m_inputObjectID.text, out TrackedObject existingTrackedObject, exactMatch: true))
            {
                ShowMessage(Debug.Log(TextType.TRACK_FAIL, m_inputObjectID.text, existingTrackedObject)); // show error message
                return;
            }

            trackedObject = CreateTrackedObject();
            TrackingHandler.Instance.AddTrackedObject(trackedObject, out bool conflicting);
            ChangeUIMode(trackedObject); // set to edit mode

            // todo: show warning if name and icon exists and would cause issues with pin color coding

            if (!conflicting) ShowMessage(Debug.Log(TextType.TRACK_SUCCESS, trackedObject));
            else ShowMessage(Debug.Log(TextType.TRACK_WARNING_CONFLICT, trackedObject));
        }

        private void ModifyTrackedObject()
        {
            TrackedObject trackedObjectToModify = m_edittingObject;
            TrackedObject newTrackedObjectValues = CreateTrackedObject();

            bool success = TrackingHandler.Instance.ModifyTrackedObject(
                trackedObjectToModify,
                newTrackedObjectValues,
                m_toggleModifyPins.isOn,
                out bool conflicting,
                out TrackedObject foundConflict);

            if (!success)
            {
                ShowMessage(Debug.Log(TextType.MODIFY_FAIL_CONFLICT, trackedObjectToModify, foundConflict));
                return;
            }

            // success
            if (conflicting) ShowMessage(Debug.Log(TextType.MODIFY_WARNING_CONFLICT, trackedObjectToModify, foundConflict));
            else ShowMessage(Debug.Log(TextType.MODIFY_SUCCESS, trackedObjectToModify));

            // todo: show warning if name and icon exists and would cause issues with pin color coding

            m_edittingObject = newTrackedObjectValues;
            int newIndex = m_dropDownTrackedList.IndexOf(m_edittingObject);
            m_dropDownTracked.SetValueWithoutNotify(newIndex);
        }

        private TrackedObject CreateTrackedObject()
        {
            Debug.Log($"setting values for {m_inputObjectID.text}");
            return new TrackedObject(
                m_inputObjectID.text,
                m_inputPinName.text,
                m_inputBlackListWord.text,
                m_pinTypeInput,
                m_pinColor,
                !m_toggleSavePin.isOn,
                m_toggleCheckPin.isOn,
                m_toggleExactMatch.isOn);
        }

        private void OnButtonTrackedModifyPressed()
        {
            if (m_dropDownTracked.value == 0) // create new entry
                TrackNewObject();
            else // modify entry
                ModifyTrackedObject();
        }

        public void OnButtonUntrackedCancelPressed()
        {
            if (m_dropDownTracked.value == 0) // cancel new entry
            {
                enabled = false;
                return;
            }
            // untrack entry
            if (!TrackingHandler.Instance.RemoveTrackedObject(m_edittingObject)) // remove entry with object id
            {
                ShowMessage(Debug.Log(TextType.UNTRACK_FAIL, m_edittingObject));
                return;
            }
            OnTrackedDropDownChanged(0);
            ShowMessage(Debug.Log(TextType.UNTRACK_SUCCESS, m_edittingObject));
        }

        // Obsolete
        public void OnPinNameChange(string newValue)
        {
            m_previewIconText.text = newValue;
        }

        public void OnPinIconDropDownChanged(int value)
        {
            if (value >= (int)Minimap.PinType.None) value++;    // since I base this on dropdown values and I excluded pintype.none, I have to increment value by 1 or -1 (if not from dropdown) to avoid getting the value of the none pin

            Minimap.PinType pinType = (Minimap.PinType)(value); // cast value to PinType enum
            m_previewIcon.sprite = PinHandler.Instance.DictionaryPinType[pinType].Item1;
            m_pinTypeInput = pinType;
        }

        public void OnTrackedDropDownChanged(int value)
        {
            string objID = string.Empty;
            // create new
            if (value != 0) objID = m_dropDownTrackedList[value].ObjectID;
            SetupUIValues(objID, true);
        }

        public void OnToggleCheckPinChanged(bool value)
        {
            m_previewIconChecked.gameObject.SetActive(value);
        }

        public void OnToggleExactMatchChanged(bool value)
        {
            // exact match
            if (value && !string.IsNullOrEmpty(m_oldObjectID)) m_inputObjectID.text = m_oldObjectID;
            m_inputObjectID.interactable = !value;
        }

        public void ShowMessage(string message)
        {
            m_messageBox.text = message;
        }

        public void ShowColorPicker()
        {
            m_edittingColor = true;
            BlockInput(true);
            GUIManager.Instance.CreateColorPicker(
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f),
                m_pinColor,             // Initial selected color in the picker
                "Pin Color",   // Caption of the picker window
                ChangeIconColor,        // Callback delegate when the color in the picker changes
                SelectColor,            // Callback delegate when the window is closed
                true                    // Whether or not the alpha channel should be editable
            );
        }

        public void ChangeIconColor(Color pickedColor)
        {
            m_pinColorBox.image.color = m_previewIcon.color = pickedColor;
        }

        public void SelectColor(Color pickedColor)
        {
            BlockInput(false);
            m_edittingColor = false;
            m_pinColorBox.image.color = m_previewIcon.color = m_pinColor = pickedColor;
        }
    }
}