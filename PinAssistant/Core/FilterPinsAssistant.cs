using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WxAxW.PinAssistant.Patches;
using WxAxW.PinAssistant.Utils;

namespace WxAxW.PinAssistant.Core
{
    internal class FilterPinsAssistant : PluginComponent
    {
        private static FilterPinsAssistant m_instance = new FilterPinsAssistant();

        private IEnumerable<Minimap.PinData> m_listFilteredOutPinsQuery;
        private List<Minimap.PinData> m_listFilteredOutPins;

        internal static FilterPinsAssistant Instance { get => m_instance; set => m_instance = value; }

        public override void Start() {}

        public override void Destroy() {}

        public override void OnEnable()
        {
            MinimapPatches.OnPinAdd += OnPinAdd;
            MinimapPatches.OnPinSetTarget += OnPinSetup;
            MinimapPatches.OnMinimapUpdatePins += OnMinimapUpdatePins;
        }

        public override void OnDisable()
        {
            MinimapPatches.OnPinAdd -= OnPinAdd;
            MinimapPatches.OnPinSetTarget -= OnPinSetup;
            MinimapPatches.OnMinimapUpdatePins -= OnMinimapUpdatePins; // do not color pins when mod is disabled
            ResetFilteredPins();
        }

        public void SearchPins(string pinNameQuery, Minimap.PinType pinTypeQuery, bool whitelist = false, bool isRegex = false)
        {
            ResetFilteredPins();
            if (isRegex)
            {
                if (!IsRegexValid(pinNameQuery)) return;

                m_listFilteredOutPinsQuery = PinHandler.Instance.MonitoredPins.Values
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

                m_listFilteredOutPinsQuery = PinHandler.Instance.MonitoredPins.Values
                    .Where(pinData =>
                    {
                        bool pinNameMatches = CompareSearch(pinData.m_name, pinNameQuery, isExact);
                        bool pinTypeMatches = PinTypeMatches(pinData.m_type, pinTypeQuery);
                        bool filterOut = pinNameMatches && pinTypeMatches;
                        return whitelist ? !filterOut : filterOut;
                    });
            }
            m_listFilteredOutPins = m_listFilteredOutPinsQuery.ToList();
            FilterOutPins(); // Immediately Filter out pins, as Minimap doesn't update all the time.
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
            // Skip if no pins are filtered out
            if (m_listFilteredOutPins == null) return;
            FilterOutPins(renderPins: true);
            m_listFilteredOutPinsQuery = null;
            m_listFilteredOutPins = null;
        }

        public void FilterOutPins(bool renderPins = false)
        {
            // Skip if no pins are filtered out
            if (m_listFilteredOutPins == null) return;
            foreach (Minimap.PinData unFilteredPin in m_listFilteredOutPins)
            {
                unFilteredPin.m_NamePinData?.PinNameGameObject?.SetActive(renderPins);
                unFilteredPin.m_uiElement?.gameObject.SetActive(renderPins); // icon and checked
            }
        }

        // Modifies all valid pins based on pin name and pin type, and replace them
        public void ReplacePins(string oldPinsName, string newPinsName, Minimap.PinType oldType, Minimap.PinType newType, bool isRegex)
        {
            Minimap.instance.m_pinUpdateRequired = true;
            Debug.Log("Renaming all matching pins");
            bool isExact;
            if (isRegex && !IsRegexValid(oldPinsName))
            {
                Debug.Error("Invalid Regex pattern!");
                return;
            }
            else
            {
                isExact = IsExact(oldPinsName, out oldPinsName);
            }

            foreach (Minimap.PinData pinData in PinHandler.Instance.MonitoredPins.Values)
            {
                bool pinNameMatches = isRegex ? Regex.IsMatch(pinData.m_name, oldPinsName, RegexOptions.IgnoreCase) : CompareSearch(pinData.m_name, oldPinsName, isExact);
                if (!pinNameMatches) continue;

                bool pinTypeMatches = PinTypeMatches(pinData.m_type, oldType);
                if (!pinTypeMatches) continue;

                Minimap.PinType actualNewType = newType == Minimap.PinType.None ? pinData.m_type : newType;

                PinGroupHandler.Instance.ModifyPin(pinData, newPinsName, actualNewType);
            }

            UpdateFilteredOutPins();
        }

        public void UpdateFilteredOutPins()
        {
            // Skip if no pins are filtered out
            if (m_listFilteredOutPinsQuery == null) return;
            Minimap.instance.m_pinUpdateRequired = true; // Immediately Filter out pins, as Minimap doesn't update all the time after update v0.220.
            m_listFilteredOutPins = m_listFilteredOutPinsQuery.ToList();
        }

        private void OnPinAdd(Minimap.PinData pin)
        {
            if (MinimapPatches.isSpecialPin) return;
            if (MinimapPatches.isManualPin) return; // do not update if it's a manual pin
            
            UpdateFilteredOutPins();
        }

        private void OnPinSetup(Minimap.PinData pin)
        {
            // Event is triggered when player has left the edit menu.
            if (pin == null) UpdateFilteredOutPins();
        }

        private void OnMinimapUpdatePins()
        {
            FilterOutPins();
        }
    }
}
