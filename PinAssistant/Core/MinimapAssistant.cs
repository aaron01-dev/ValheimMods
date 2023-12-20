using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using WxAxW.PinAssistant.Patches;
using WxAxW.PinAssistant.Utils;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Core
{
    internal class MinimapAssistant : PluginComponent
    {
        private class PinGroup
        {
            private readonly List<Minimap.PinData> m_pins = new List<Minimap.PinData>();
            private string pinName = string.Empty;
            private Minimap.PinType pinType = Minimap.PinType.None;
            private Color m_pinColor;
            private Color m_pinColorShared;

            public Color PinColor
            {
                get => m_pinColor;
                set
                {
                    m_pinColor = value;
                    m_pinColorShared = new Color(value.r * 0.7f, value.g * 0.7f, value.b * 0.7f, value.a * 0.8f);
                }
            }

            public PinGroup(string pinName, Minimap.PinType pinType, Color pinColor)
            {
                SetValues(pinName, pinType, pinColor);
            }

            public void ApplyColor()
            {
                if (m_pinColor == Color.white) return;
                Color pinFadeColor = m_pinColorShared;
                pinFadeColor.a *= Minimap.instance.m_sharedMapDataFade;
                foreach (var pin in m_pins)
                {
                    Image currPinIcon = pin.m_iconElement;
                    if (currPinIcon == null) continue;

                    currPinIcon.color = pin.m_ownerID == 0 ? m_pinColor : pinFadeColor;
                }
            }

            public void ResetColor()
            {
                m_pinColor = Color.white;
            }

            public void ModifyPins(string newName, Minimap.PinType newType)
            {
                if (!pinName.Equals(newName))
                {
                    pinName = newName;
                    foreach (var pin in m_pins)
                    {
                        SetPinName(pin, pinName);
                    }
                }
                if (pinType != newType)
                {
                    pinType = newType;
                    Sprite pinSprite = Minimap.instance.GetSprite(pinType);
                    foreach (var pin in m_pins)
                    {
                        SetPinType(pin, pinType, pinSprite);
                    }
                }
            }

            public void Add(Minimap.PinData pin)
            {
                m_pins.Add(pin);
            }

            public void AddRange(PinGroup pinGroup)
            {
                pinGroup.ModifyPins(pinName, pinType);
                AddFormattedRange(pinGroup);
            }
            
            public void AddFormattedRange(PinGroup pinGroup)
            {
                m_pins.AddRange(pinGroup.m_pins);
                pinGroup.Clear();
            }

            public bool Remove(Minimap.PinData pin)
            {
                if (m_pins.Remove(pin))
                {
                    Debug.Log("Removed pin");
                    return true;
                }
                return false;
            }

            public void Clear()
            {
                m_pins.Clear();
            }

            public void SetValues(string pinName, Minimap.PinType pinType, Color pinColor)
            {
                this.pinName = pinName;
                this.pinType = pinType;
                PinColor = pinColor;
            }
        }

        private static MinimapAssistant m_instance = new MinimapAssistant();

        private readonly Dictionary<string, PinGroup> m_pins = new Dictionary<string, PinGroup>();

        private IEnumerable<Minimap.PinData> m_listUnfilteredPinsQuery;
        private List<Minimap.PinData> m_listUnfilteredPins;

        private Dictionary<Minimap.PinType, Tuple<Sprite, string>> m_dictionaryPinType = new Dictionary<Minimap.PinType, Tuple<Sprite, string>>();
        private bool m_dictionaryPinTypePopulated = false;

        public Action OnDictionaryPinTypePopulated;

        public static MinimapAssistant Instance { get => m_instance; private set => m_instance = value; }
        public Dictionary<Minimap.PinType, Tuple<Sprite, string>> DictionaryPinType { get => m_dictionaryPinType; set => m_dictionaryPinType = value; }
        public bool DictionaryPinTypePopulated { get => m_dictionaryPinTypePopulated; set => m_dictionaryPinTypePopulated = value; }

        public override void Start()
        {
            MinimapPatches.OnPinAdd += OnPinAdd;
            MinimapPatches.OnPinRemove += OnPinRemove;
            MinimapPatches.OnPinSetTarget += OnPinSetup;
            MinimapPatches.OnPinUpdate += OnPinUpdate;
            MinimapManager.OnVanillaMapAvailable += PopulateIcons;
        }

        public override void Destroy()
        {
            MinimapPatches.OnPinAdd -= OnPinAdd;
            MinimapPatches.OnPinRemove -= OnPinRemove;
            MinimapPatches.OnPinSetTarget -= OnPinSetup;
            MinimapPatches.OnPinUpdate -= OnPinUpdate;
            m_instance = null;
        }

        public override void OnEnable()
        {
            OnTrackedObjectsReload(TrackingAssistant.Instance.TrackedObjects);

            // check if instance exists
            TrackingAssistant.Instance.OnTrackedObjectAdd += OnTrackedObjectAdd;
            TrackingAssistant.Instance.OnTrackedObjectRemove += OnTrackedObjectRemove;
            TrackingAssistant.Instance.OnTrackedObjectUpdate += OnTrackedObjectUpdate;
            TrackingAssistant.Instance.OnTrackedObjectsReload += OnTrackedObjectsReload;
        }

        public override void OnDisable()
        {
            TrackingAssistant.Instance.OnTrackedObjectAdd -= OnTrackedObjectAdd;
            TrackingAssistant.Instance.OnTrackedObjectRemove -= OnTrackedObjectRemove;
            TrackingAssistant.Instance.OnTrackedObjectUpdate -= OnTrackedObjectUpdate;
            TrackingAssistant.Instance.OnTrackedObjectsReload -= OnTrackedObjectsReload;
            ResetFilteredPins();
        }

        private void PopulateIcons()
        {
            if (Minimap.instance == null) return;
            Array pinTypes = Enum.GetValues(typeof(Minimap.PinType));
            m_dictionaryPinType.Add(Minimap.PinType.None, new Tuple<Sprite, string>(null, "None"));
            foreach (Minimap.PinType pinType in pinTypes)
            {
                if (pinType == Minimap.PinType.None) continue;
                Sprite pinIcon = Minimap.instance.GetSprite(pinType);
                string iconName = FormatSpriteName(pinIcon.name);
                m_dictionaryPinType.Add(pinType, new Tuple<Sprite, string>(pinIcon, iconName));
            }
            MinimapManager.OnVanillaMapAvailable -= PopulateIcons;
            m_dictionaryPinTypePopulated = true;
            OnDictionaryPinTypePopulated?.Invoke();
        }
        private string FormatSpriteName(string sprName)
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

        public void ColorPins()
        {
            foreach (var kvp in m_pins)
            {
                kvp.Value.ApplyColor();
            }
        }

        public void SearchPins(string pinNameQuery, Minimap.PinType pinTypeQuery, bool whitelist = false, bool isRegex = false)
        {
            ResetFilteredPins();
            if (isRegex)
            {
                if (!IsRegexValid(pinNameQuery)) return;

                m_listUnfilteredPinsQuery = TrackingAssistant.Instance.Pins.Values
                    .Where(pinData =>
                    {
                        bool pinNameMatches = Regex.IsMatch(pinData.m_name, pinNameQuery, RegexOptions.IgnoreCase);
                        bool pinTypeMatches = PinTypeMatches(pinData.m_type, pinTypeQuery);
                        bool filterOut = pinNameMatches && pinTypeMatches;
                        return whitelist ? !filterOut : filterOut;
                    });
            }
            else
            {
                bool isExact = IsExact(pinNameQuery, out pinNameQuery);

                m_listUnfilteredPinsQuery = TrackingAssistant.Instance.Pins.Values
                    .Where(pinData =>
                    {
                        bool pinNameMatches = CompareSearch(pinData.m_name, pinNameQuery, isExact);
                        bool pinTypeMatches = PinTypeMatches(pinData.m_type, pinTypeQuery);
                        bool filterOut = pinNameMatches && pinTypeMatches;
                        return whitelist ? !filterOut : filterOut;
                    });
            }
            m_listUnfilteredPins = m_listUnfilteredPinsQuery.ToList();
        }

        private bool IsRegexValid(string pinNameQuery)
        {
            try
            {
                Regex.Match("", pinNameQuery); // check if regex is valid
                return true;
            }
            catch (Exception)
            {
                Debug.Warning("Invalid RegEx Pattern!");
                return false;
            }
        }

        private bool IsExact(string pinNameQuery, out string trimmedString)
        {
            trimmedString = pinNameQuery;
            // Define a regular expression pattern to match strings with double quotes at the front and back
            string pattern = "^\".*\"$";

            // Use Regex.IsMatch to check if the input matches the pattern
            bool isExact = Regex.IsMatch(pinNameQuery, pattern) || string.IsNullOrEmpty(pinNameQuery);
            if (isExact) trimmedString = pinNameQuery.Trim('"');
            return isExact;
        }

        private bool PinTypeMatches(Minimap.PinType pinType, Minimap.PinType pinTypeQuery)
        {
            if (pinTypeQuery == Minimap.PinType.None) return true; // always return true if `None` cause None = every icons
            return pinType == pinTypeQuery;
        }

        private bool CompareSearch(string foundPin, string query, bool isExact = false)
        {
            if (isExact) return foundPin.Equals(query, StringComparison.OrdinalIgnoreCase);
            else return foundPin.IndexOf(query, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public void ResetFilteredPins()
        {
            if (m_listUnfilteredPinsQuery == null) return;
            FilterPins(renderPins: true);
            m_listUnfilteredPinsQuery = null;
            m_listUnfilteredPins = null;
        }

        public void FilterPins()
        {
            if (m_listUnfilteredPinsQuery == null) return;
            FilterPins(renderPins: false);
        }

        public void FilterPins(bool renderPins)
        {
            foreach (Minimap.PinData unFilteredPin in m_listUnfilteredPins)
            {
                unFilteredPin.m_NamePinData?.PinNameGameObject?.SetActive(renderPins);
                unFilteredPin.m_uiElement?.gameObject.SetActive(renderPins); // icon and checked
            }
        }

        private PinGroup InitializeKey(Minimap.PinData newPin, Color pinColor)
        {
            return InitializeKey(GetPinKey(newPin), newPin.m_name, newPin.m_type, pinColor);
        }

        private PinGroup InitializeKey(string key, string pinName, Minimap.PinType pinType, Color pinColor, bool forceChangeColor = false)
        {
            if (!m_pins.TryGetValue(key, out PinGroup foundPinGroup))
            {
                Debug.Log($"Created colored pin group for {key}");
                PinGroup newPinGroup = new PinGroup(pinName, pinType, pinColor);
                m_pins.Add(key, newPinGroup);
                return newPinGroup;
            }

            if (forceChangeColor)
            {
                Debug.Log($"Colored pin group named, '{key}' exists, Changing the color only instead.");
                foundPinGroup.PinColor = pinColor;
            }
            return foundPinGroup;
        }

        public void ModifyPin(Minimap.PinData pinData, string newName, Minimap.PinType newType)
        {
            ModifyPin(pinData, pinData.m_name, newName, pinData.m_type, newType);
        }

        public void ModifyPin(Minimap.PinData pinData, string oldName, string newName, Minimap.PinType oldType, Minimap.PinType newType)
        {
            Debug.Log($"Modifying {oldName} | {oldType} to {newName} | {newType}");
            if (oldName.Equals(newName) && oldType == newType) return;

            string oldPinKey = GetPinKey(oldName, oldType);
            if (!m_pins.TryGetValue(oldPinKey, out PinGroup foundPinGroup))
            {
                Debug.Error("Pin group not found, contact dev");
                return;
            }
            if (!foundPinGroup.Remove(pinData))
            {
                Debug.Error("Pin not found in group, contact dev");
            }
            if (!oldName.Equals(newName)) SetPinName(pinData, newName);
            if (!oldType.Equals(newType)) SetPinType(pinData, newType);
            PinAdd(pinData);
        }

        public void ModifyPins(string oldPinsQuery, string newPinsName, Minimap.PinType oldType, Minimap.PinType newType, bool isRegex)
        {
            Debug.Log("Renaming all matching pins");
            bool isExact;
            if (isRegex && !IsRegexValid(oldPinsQuery))
            {
                Debug.Error("Invalid Regex pattern!");
                return;
            }
            else
            {
                isExact = IsExact(oldPinsQuery, out oldPinsQuery);
            }

            foreach (Minimap.PinData pinData in TrackingAssistant.Instance.Pins.Values)
            {
                bool pinNameMatches = isRegex ? Regex.IsMatch(pinData.m_name, oldPinsQuery, RegexOptions.IgnoreCase) : CompareSearch(pinData.m_name, oldPinsQuery, isExact);
                if (!pinNameMatches) continue;

                bool pinTypeMatches = PinTypeMatches(pinData.m_type, oldType);
                if (!pinTypeMatches) continue;
                
                Minimap.PinType actualNewType = newType == Minimap.PinType.None ? pinData.m_type : newType;
                
                ModifyPin(pinData, newPinsName, actualNewType);
            }

            UpdateUnfilteredPins();
        }

        private void TransferPinGroup(string oldPinsName, string newPinsName, Minimap.PinType oldType, Minimap.PinType newType)
        {
            if (newType == Minimap.PinType.None) return;
            if (oldPinsName.Equals(newPinsName) && oldType == newType) return;
            string newPinsKey = GetPinKey(newPinsName, newType);
            string oldPinsKey = GetPinKey(oldPinsName, oldType);
            if (!m_pins.ContainsKey(oldPinsKey)) return;
            PinGroup pinGroupToMerge = InitializeKey(newPinsKey, newPinsName, newType, Color.white);

            if (!m_pins.TryGetValue(oldPinsKey, out PinGroup oldPinGroup))
            {
                Debug.Log("No pins exists with current pin name and type.");
                return;
            }
            oldPinGroup.ModifyPins(newPinsName, newType);
            pinGroupToMerge.AddFormattedRange(oldPinGroup);
        }
        
        private string GetPinKey(Minimap.PinData pinData)
        {
            return GetPinKey(pinData.m_name, pinData.m_type);
        }
        private string GetPinKey(string pinName, Minimap.PinType pinType)
        {
            string formattedPinKey = pinName.ToLower() + "_" + pinType.ToString();
            return formattedPinKey;
        }

        public void UpdateUnfilteredPins()
        {
            if (m_listUnfilteredPins == null) return;
            m_listUnfilteredPins = m_listUnfilteredPinsQuery.ToList();
        }

        private void PinAdd(Minimap.PinData pin)
        {
            PinGroup initializedPinGroup = InitializeKey(pin, Color.white);
            initializedPinGroup.Add(pin);
        }

        private void OnPinAdd(Minimap.PinData pin)
        {
            if (MinimapPatches.isSpecialPin) return;
            PinAdd(pin);
            if (!MinimapPatches.isManualPin) UpdateUnfilteredPins();
        }

        private void OnPinRemove(Minimap.PinData pin)
        {
            if (!m_pins.TryGetValue(GetPinKey(pin), out PinGroup oldPinGroup)) return;
            oldPinGroup.Remove(pin);
        }

        private void OnPinSetup(Minimap.PinData pin)
        {
            if (MinimapPatches.m_edittingPin == null) return;
            if (pin == MinimapPatches.m_edittingPin) return;
            UpdateUnfilteredPins();
        }

        private void OnPinUpdate()
        {
            if (MinimapPatches.m_edittingPin == null) return;
            string oldName = MinimapPatches.m_edittingPinInitial.m_name;
            string newName = MinimapPatches.m_edittingPin.m_name;
            Minimap.PinType oldType = MinimapPatches.m_edittingPinInitial.m_type;
            Minimap.PinType newType = MinimapPatches.m_edittingPin.m_type;
            Debug.Log($"Updating pin from, {oldName} | {oldType}, to {newName} | {newType}");

            ModifyPin(MinimapPatches.m_edittingPin, oldName, newName, oldType, newType);
        }

        private void OnTrackedObjectAdd(TrackedObject trackedObject)
        {
            string pinName = trackedObject.Name;
            Minimap.PinType pinType = trackedObject.Icon;
            Color pinColor = trackedObject.PinColor;

            Debug.Log($"Initializing pin name storage for tracked object, {trackedObject.ObjectID}.");
            string pinKey = GetPinKey(pinName, pinType);
            
            InitializeKey(pinKey, pinName, pinType, pinColor, forceChangeColor: true);
        }

        private void OnTrackedObjectRemove(TrackedObject trackedObject)
        {
            string pinKey = GetPinKey(trackedObject.Name, trackedObject.Icon);
            if (!m_pins.TryGetValue(pinKey, out PinGroup pinGroup))
            {
                Debug.Error("Failed to remove group, contact dev!");
                return;
            }
            Debug.Log($"Removing color for {trackedObject.ObjectID}");
            pinGroup.PinColor = Color.white; // "remove" by changing the color to white
        }

        private void OnTrackedObjectUpdate(TrackedObject trackedObject, TrackedObject newTrackedObject, bool modifyPins)
        {
            string oldPinName = trackedObject.Name;
            Minimap.PinType oldPinType = trackedObject.Icon;
            string newPinName = newTrackedObject.Name;
            Minimap.PinType newPinType = newTrackedObject.Icon;
            if (oldPinName.Equals(newPinName) && oldPinType == newPinType) return;
            
            string oldPinKey = GetPinKey(oldPinName, oldPinType);
            if (!m_pins.TryGetValue(oldPinKey, out PinGroup pinGroup))
            {
                Debug.Error("Failed to update group, contact dev!");
                return;
            }
            if (modifyPins) TransferPinGroup(oldPinName, newPinName, oldPinType, newPinType);

            pinGroup.ResetColor();
            OnTrackedObjectAdd(newTrackedObject);
        }

        private void OnTrackedObjectsReload(LooseDictionary<TrackedObject> trackedObjects)
        {
            foreach (var item in m_pins)
            {
                item.Value.ResetColor();
            }

            foreach (var item in trackedObjects.AltDictionary.Values)
            {
                OnTrackedObjectAdd(item.Value);
            }
            Debug.Log("Minimap Pin color reloaded");
        }

        public static void SetPinName(Minimap.PinData pin, string newName)
        {
            pin.m_name = newName;
            pin.m_NamePinData?.PinNameText?.SetText(newName);
        }

        public static void SetPinType(Minimap.PinData pin, Minimap.PinType newPinType, Sprite pinSprite = null)
        {
            pin.m_type = newPinType;
            if (pinSprite == null) pinSprite = Minimap.instance.GetSprite(newPinType);
            pin.m_icon = pinSprite;
            if (pin.m_iconElement == null) return; // skip changing visual if it's out of visual range
            pin.m_iconElement.sprite = pin.m_icon;
        }
    }
}