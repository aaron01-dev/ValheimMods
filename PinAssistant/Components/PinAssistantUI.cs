using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WxAxW.PinAssistant.Utils;
using static Mono.Security.X509.X520;
using static UnityEngine.GraphicsBuffer;
using static WxAxW.PinAssistant.Components.PinAssistant;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Components
{
    internal class PinAssistantUI : MonoBehaviour
    {
        private static PinAssistantUI m_instance;
        public static PinAssistantUI Instance => m_instance;

        private readonly string[] m_modifiableText = new string[] {
            TextAttribute.Get(TextType.HEADER_TRACK),
            TextAttribute.Get(TextType.HEADER_MODIFY),
            TextAttribute.Get(TextType.BUTTON_TRACK),
            TextAttribute.Get(TextType.BUTTON_MODIFY),
            TextAttribute.Get(TextType.BUTTON_CANCEL),
            TextAttribute.Get(TextType.BUTTON_UNTRACK)
        };

#pragma warning disable 0649
        [SerializeField] private Image m_panel;
        [SerializeField] private Transform m_header;
        [SerializeField] private TMP_Text m_headerText;
        [SerializeField] private Image m_body;

        [SerializeField] private Image m_previewIcon;
        [SerializeField] private Image m_previewIconChecked;
        [SerializeField] private TMP_Text m_previewIconText;
        [SerializeField] private TMP_InputField m_inputPinName;
        [SerializeField] private TMP_InputField m_inputObjectID;
        [SerializeField] private TMP_InputField m_inputBlackListWord;
        [SerializeField] private TMP_Dropdown m_dropDownPinIcon;
        [SerializeField] private Toggle m_toggleSavePin;
        [SerializeField] private Toggle m_toggleCheckPin;
        [SerializeField] private Toggle m_toggleExactMatch;
        [SerializeField] private TMP_Dropdown m_dropDownTracked;
        [SerializeField] private TMP_Text m_messageBox;
        [SerializeField] private Button m_buttonTrackModify;
        [SerializeField] private TMP_Text m_buttonTrackModifyText;
        [SerializeField] private Button m_buttonUntrackCancel;
        [SerializeField] private TMP_Text m_buttonUntrackCancelText;
        [SerializeField] private Transform m_creditRow;
        [SerializeField] private TMP_Text m_versionNumber;
#pragma warning restore 0649

        private bool isActive = false;
        private bool editMode = false;
        private string m_oldObjectID = string.Empty; // if creating a new entry, this is for autofill area, used by "exactMatch" toggle to lock the user from editing inputObjectID field
        private TrackedObject m_edittingObject; // when editing an entry, used to have a data to compare values to the new values
        private readonly List<TrackedObject> m_dropDownTrackedList = new List<TrackedObject>(); // drop down object equivalent to dropdown's values
        private readonly Dictionary<Minimap.PinType, Sprite> m_dictionaryPinIcons = new Dictionary<Minimap.PinType, Sprite>(); // dictionary of sprites
        private Minimap.PinType m_pinTypeInput;

        public static void Init(AssetBundle assetBundle)
        {
            GameObject prefab = assetBundle.LoadAsset<GameObject>("Assets/ObjPinAssistantUI.prefab");
            m_instance = SpawnUI(prefab);
            m_instance.PopulateIcons();
            m_instance.PopulateDropdownTracked();
            m_instance.ApplyAllComponents();
            m_instance.m_versionNumber.text = $"v{Plugin.PluginVersion}";
            m_instance.m_previewIconChecked.sprite = GUIManager.Instance.GetSprite("mapicon_checked");
            m_instance.m_previewIconText.text = string.Empty; // reset
            m_instance.m_previewIconChecked.gameObject.SetActive(false); // reset
        }

#pragma warning disable IDE0051 // Remove unused private members

        private void Awake()
#pragma warning restore IDE0051 // Remove unused private members
        {
            m_inputPinName.onValueChanged.AddListener(OnPinNameChange);
            m_buttonTrackModify.onClick.AddListener(OnButtonTrackedModifyPressed);
            m_buttonUntrackCancel.onClick.AddListener(OnButtonUntrackedCancelPressed);
            m_dropDownPinIcon.onValueChanged.AddListener(OnPinIconDropDownChanged);
            m_dropDownTracked.onValueChanged.AddListener(OnTrackedDropDownChanged);
            m_toggleCheckPin.onValueChanged.AddListener(OnToggleCheckPinChanged);
            m_toggleExactMatch.onValueChanged.AddListener(OnToggleExactMatchChanged);
            PinAssistant.Instance.LoadedTrackedObjects += PopulateDropdownTracked;
            //m_buttonUntrackCancel.onClick.AddListener()
        }

        private void Update()
        {
            // keybind to close ui
            if (isActive)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    SetUIActive(false);
                if (Input.GetKeyDown(KeyCode.Return))
                    OnButtonTrackedModifyPressed();
            }
        }

        private void OnDestroy()
        {
            m_inputPinName.onValueChanged.RemoveListener(OnPinNameChange);
            m_buttonTrackModify.onClick.RemoveListener(OnButtonTrackedModifyPressed);
            m_buttonUntrackCancel.onClick.RemoveListener(OnButtonUntrackedCancelPressed);
            m_dropDownPinIcon.onValueChanged.RemoveListener(OnPinIconDropDownChanged);
            m_dropDownTracked.onValueChanged.RemoveListener(OnTrackedDropDownChanged);
            m_toggleCheckPin.onValueChanged.RemoveListener(OnToggleCheckPinChanged);
            m_toggleExactMatch.onValueChanged.RemoveListener(OnToggleExactMatchChanged);
            PinAssistant.Instance.LoadedTrackedObjects -= PopulateDropdownTracked;
        }

        private static PinAssistantUI SpawnUI(GameObject prefab)
        {
            GameObject inactive = new GameObject("inactive");
            inactive.SetActive(false);
            PinAssistantUI ui = Instantiate(prefab, GUIManager.CustomGUIFront.transform, false).GetComponent<PinAssistantUI>();
            //ui.gameObject.SetActive(false);
            ui.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
            ui.gameObject.SetActive(false);
            Destroy(inactive);
            return ui;
        }

        public void ApplyAllComponents()
        {
            m_panel.color = Color.white;
            TMPGUIManager.Instance.ApplyWoodpanel(m_panel);

            TMPGUIManager.Instance.ApplyTextStyle(m_header.GetChild(0).GetComponent<TMP_Text>(), TMPGUIManager.Instance.NorseBold, GUIManager.Instance.ValheimOrange, 36);

            // apply text to everything as a pre setup for objects not referenced
            foreach (TMP_Text text in m_body.GetComponentsInChildren<TMP_Text>())
            {
                TMPGUIManager.Instance.ApplyTextStyle(text, TMPGUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimOrange, 20, true);
            }

            foreach (TMP_Text text in m_creditRow.GetComponentsInChildren<TMP_Text>())
            {
                TMPGUIManager.Instance.ApplyTextStyle(text, TMPGUIManager.Instance.AveriaSerifBold, GUIManager.Instance.ValheimYellow, 12, true);
            }

            // change preview icon name style
            TMPGUIManager.Instance.ApplyTextStyle(m_previewIconText, TMPGUIManager.Instance.NorseBold, Color.white, 16);

            foreach (TMP_InputField inputField in new TMP_InputField[] { m_inputPinName, m_inputObjectID, m_inputBlackListWord })
            {
                TMPGUIManager.Instance.ApplyInputFieldStyle(inputField, 16);
            }

            foreach (Toggle toggle in new Toggle[] { m_toggleSavePin, m_toggleCheckPin, m_toggleExactMatch })
            {
                GUIManager.Instance.ApplyToogleStyle(toggle);
                ApplyToggleTextStyle(toggle, 16);
            }

            foreach (Button button in new Button[] { m_buttonTrackModify, m_buttonUntrackCancel })
            {
                TMPGUIManager.Instance.ApplyButtonStyle(button, 20);
            }

            TMPGUIManager.Instance.ApplyDropdownStyle(m_dropDownPinIcon);
            TMPGUIManager.Instance.ApplyDropdownStyle(m_dropDownTracked);
        }

        public void ApplyToggleTextStyle(Toggle toggle, int fontSize)
        {
            TMP_Text componentInChildren = toggle.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if ((bool)componentInChildren)
            {
                TMPGUIManager.Instance.ApplyTextStyle(componentInChildren, GUIManager.Instance.ValheimOrange, fontSize);
                componentInChildren.alignment = TextAlignmentOptions.Left; // todo: double check if center means middle center
            }
        }

        public void PopulateIcons()
        {
            Array pinTypes = Enum.GetValues(typeof(Minimap.PinType));
            foreach (Minimap.PinType pinType in pinTypes)
            {
                if (pinType == Minimap.PinType.None) continue;
                Sprite pinIcon = Minimap.instance.GetSprite(pinType);
                m_dictionaryPinIcons.Add(pinType, pinIcon);
                string iconName = FormatSpriteName(pinIcon.name);
                m_dropDownPinIcon.options.Add(new TMP_Dropdown.OptionData(iconName));
            }
        }

        public string FormatSpriteName(string sprName)
        {
            // Remove alphanumeric values ex. "SunkenCrypt4" -> "SunkenCrypt"
            //sprName = Regex.Replace(name, @"\d", string.Empty);

            // Remove mapicon prefix
            sprName = Regex.Replace(sprName, "mapicon_", string.Empty);
            if (sprName.IndexOf("_32") != -1) sprName = Regex.Replace(sprName, "_32", string.Empty); // format for player_32
            if (sprName.IndexOf("_colored") != -1) sprName = Regex.Replace(sprName, "_colored", string.Empty); // format for boss_colored
            sprName = Regex.Replace(sprName, @"(^\w)|(\s\w)", m => m.Value.ToUpper());

            return sprName;
        }


        public void PopulateDropdownTracked()
        {
            m_dropDownTracked.ClearOptions();
            m_dropDownTrackedList.Clear();
            m_dropDownTracked.options.Add(new TMP_Dropdown.OptionData("New..."));
            m_dropDownTrackedList.Add(default);

            foreach (LooseDictionary<TrackedObject>.TrieNode node in TrackedObjects.altDictionary.Values)
            {
                TrackedObject trackedObject = node.Value;
                m_dropDownTrackedList.Add(trackedObject);
                m_dropDownTracked.options.Add(new TMP_Dropdown.OptionData(trackedObject.Name));
            }
            m_dropDownTracked.value = 0;
            Debug.Log(TextType.OBJECTS_DROPDOWN_LOADED);
        }

        public void SetUIActive(bool value)
        {
            isActive = value;
            GUIManager.BlockInput(value);
            gameObject.SetActive(value);

            if (value) return; // reset on open
            m_dropDownTracked.SetValueWithoutNotify(0);
            m_messageBox.text = string.Empty;
            m_edittingObject = null;
        }

        public void SetupTrackObject(GameObject obj)
        {
            SetUIActive(!isActive);

            // check if opening or closing
            if (!isActive) return;

            // setup values
            string name = obj?.name ?? string.Empty;
            SetupUIValues(name);
        }

        public void SetupUIValues(string _objectID, bool _exactMatch = false)
        {
            m_oldObjectID = string.Empty; // reset
            // check if object id loosely matches a key in dictionary
            string formattedName = string.Empty;
            string objIDName = _objectID;
            string blackListWords = "";
            bool isSaved = true, isChecked = false, IsExactMatchOnly = false;
            TrackedObject trackedObject = null;
            int pinType = (int)Minimap.PinType.Icon3;
            if (!string.IsNullOrEmpty(objIDName))
            {
                if (TrackedObjects.TryGetValueLoose(objIDName, out trackedObject, _exactMatch))
                {
                    formattedName = trackedObject.Name;
                    objIDName = trackedObject.ObjectID;
                    blackListWords = trackedObject.BlackListWords;
                    int actualPinType = (int)trackedObject.Icon;
                    pinType = actualPinType >= (int)Minimap.PinType.None ? actualPinType - 1 : actualPinType;
                    isSaved = trackedObject.Save;
                    isChecked = trackedObject.IsChecked;
                    IsExactMatchOnly = trackedObject.IsExactMatchOnly;
                    m_dropDownTracked.SetValueWithoutNotify(m_dropDownTrackedList.IndexOf(trackedObject)); // change dropdown input
                    if (IsExactMatchOnly) m_oldObjectID = objIDName; // to allow the user to modify the object id when they created an entry that's not "exact match"
                }
                else
                {
                    formattedName = PinAssistant.Instance.FormatObjectName(objIDName);
                    m_oldObjectID = objIDName; // to allow the user to modify the object id when they created an entry that's not "exact match"
                    IsExactMatchOnly = true;
                }
                // m_previewIcon.sprite = AutoPinning.Instance.TrackedObjects[newValue].Icon;
            }

            m_inputObjectID.text = objIDName;
            m_inputPinName.text = formattedName;
            m_inputBlackListWord.text = blackListWords;
            m_dropDownPinIcon.value = (int)pinType;

            m_toggleSavePin.isOn = !isSaved;
            m_toggleCheckPin.isOn = isChecked;
            m_toggleExactMatch.isOn = IsExactMatchOnly;
            ChangeEditMode(trackedObject);
        }

        private void ChangeEditMode(TrackedObject trackedObject = null)
        {
            editMode = false;
            if (trackedObject != null)
            {
                editMode = true;
                m_edittingObject = trackedObject;
                m_dropDownTracked.SetValueWithoutNotify(m_dropDownTrackedList.IndexOf(trackedObject)); // switch to specified drop down
            }
            int index = !editMode ? 0 : 1;
            m_headerText.text = m_modifiableText[index];

            m_buttonTrackModifyText.text = m_modifiableText[index + 2];
            m_buttonUntrackCancelText.text = m_modifiableText[index + 4];
        }

        private void RemoveDropDownTracked(int index = -1)
        {
            int value = index == -1 ? m_dropDownTracked.value : index;
            m_dropDownTracked.options.RemoveAt(value);
            m_dropDownTrackedList.RemoveAt(value);
        }

        private void AddDropDownTracked(TrackedObject newTrackedObject, int index = -1)
        {
            if (index != -1)
            {
                m_dropDownTrackedList.Insert(index, newTrackedObject);
                m_dropDownTracked.options.Insert(index, new TMP_Dropdown.OptionData(newTrackedObject.Name));
            }
            else
            {
                m_dropDownTrackedList.Add(newTrackedObject);
                m_dropDownTracked.options.Add(new TMP_Dropdown.OptionData(newTrackedObject.Name));
            }
        }

        private void TrackNewObject()
        {
            TrackedObject trackedObject;
            if (string.IsNullOrEmpty(m_inputObjectID.text))
            {
                ShowMessage(Debug.Log(TextType.TRACK_INVALID));
                return;
            }

            // double check if the objectID already exists or is conflicting with something
            if (TrackedObjects.TryGetValueLoose(m_inputObjectID.text, out TrackedObject existingTrackedObject, true))
            {
                ShowMessage(Debug.Log(TextType.TRACK_FAIL, m_inputObjectID.text, existingTrackedObject)); // show error message
                return;
            }

            trackedObject = new TrackedObject();
            SetTrackedObjectValuesToUIValues(trackedObject); // fill values
            PinAssistant.Instance.AddTrackedObject(m_inputObjectID.text, trackedObject, out bool conflicting, m_inputBlackListWord.text, m_toggleExactMatch.isOn);
            AddDropDownTracked(trackedObject);  // add dropdown with object name
            ChangeEditMode(trackedObject); // set to edit mode

            if (!conflicting) ShowMessage(Debug.Log(TextType.TRACK_SUCCESS, trackedObject));
            else ShowMessage(Debug.Log(TextType.TRACK_WARNING_CONFLICT, trackedObject));
        }

        private void ModifyTrackedObject()
        {
            TrackedObject trackedObject = m_edittingObject;
            bool conflicting = false;
            if (m_edittingObject.ObjectID.Equals(m_inputObjectID.text)) // check if the ID is still the same meaning same key still
            {
                // modify only if exact match or blacklsit changed cause dictionary needs these two, if false just modify tracked object cause class is reference type so editing tracked object here edits the one in the dictionary too.
                if (trackedObject.IsExactMatchOnly != m_toggleCheckPin.isOn || !trackedObject.BlackListWords.Equals(m_inputBlackListWord.text))    
                    TrackedObjects.Modify(trackedObject.ObjectID, trackedObject, m_toggleExactMatch.isOn, m_inputBlackListWord.text, true);
            }
            else // tracked object's ID has changed therefore delete the old TrackedObjects key and its value then create a new key with modified key(ID) with same value(class)
            {
                // Remove the old entry from the dictionary
                PinAssistant.Instance.RemoveTrackedObject(trackedObject.ObjectID, true);

                // check if it's conflicting with a different ID
                if (TrackedObjects.TryGetValueLoose(m_inputObjectID.text, out TrackedObject conflictTrackedObject, m_toggleExactMatch.isOn)) // if new ID already exists in the dictionary, cancel method;
                {
                    ShowMessage(Debug.Log(TextType.MODIFY_WARNING_CONFLICT, trackedObject, conflictTrackedObject));
                    conflicting = true;
                }

                // Add the new entry to the dictionary
                PinAssistant.Instance.AddTrackedObject(m_inputObjectID.text, trackedObject, out _, m_inputBlackListWord.text, m_toggleExactMatch.isOn);
            }
            // Since entry in dropdown exists, just change the name in the UI dropdown.
            m_dropDownTracked.options[m_dropDownTracked.value].text = m_inputPinName.text;
            SetTrackedObjectValuesToUIValues(trackedObject);
            if (!conflicting) ShowMessage(Debug.Log(TextType.MODIFY_SUCCESS, trackedObject));
        }

        private void SetTrackedObjectValuesToUIValues(TrackedObject trackedObject)
        {
            trackedObject?.SetValues(m_inputObjectID.text, m_inputPinName.text, m_inputBlackListWord.text, m_pinTypeInput, !m_toggleSavePin.isOn, m_toggleCheckPin.isOn, m_toggleExactMatch.isOn);
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
                SetUIActive(false);
                return;
            }
            // untrack entry
            if (!PinAssistant.Instance.RemoveTrackedObject(m_edittingObject.ObjectID, true)) // remove entry with object id
            {
                ShowMessage(Debug.Log(TextType.UNTRACK_FAIL, m_edittingObject));
                return;
            }
            RemoveDropDownTracked(m_dropDownTracked.value);
            m_dropDownTracked.value = 0;
            ShowMessage(Debug.Log(TextType.UNTRACK_SUCCESS, m_edittingObject));
        }

        // Obsolete
        public void OnPinNameChange(string newValue)
        {
            m_previewIconText.text = newValue;
        }

        private void OnPinIconDropDownChanged(int value)
        {
            if (value >= (int)Minimap.PinType.None) value++;    // since I base this on dropdown values and I excluded pintype.none, I have to increment value by 1 or -1 (if not from dropdown) to avoid getting the value of the none pin

            Minimap.PinType pinType = (Minimap.PinType)(value); // cast value to PinType enum
            m_previewIcon.sprite = m_dictionaryPinIcons[pinType];
            m_pinTypeInput = pinType;
        }

        private void OnTrackedDropDownChanged(int value)
        {
            string objID = string.Empty;
            // create new
            if (value != 0) objID = m_dropDownTrackedList[value].ObjectID;
            SetupUIValues(objID, true);
        }

        private void OnToggleCheckPinChanged(bool value)
        {
            m_previewIconChecked.gameObject.SetActive(value);
        }

        private void OnToggleExactMatchChanged(bool value)
        {
            // exact match
            if (value && !string.IsNullOrEmpty(m_oldObjectID)) m_inputObjectID.text = m_oldObjectID;
            m_inputObjectID.interactable = !value;
        }

        public void ShowMessage(string message)
        {
            m_messageBox.text = message;
        }

        /*
         * "Raspberries
         * [<color=yellow><b>E</b></color>] Pick up"
         */
    }
}