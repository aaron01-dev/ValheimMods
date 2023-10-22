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
    internal class MinimapAssistant : Component
    {
        private class PinExpand
        {
            private readonly List<Minimap.PinData> m_pins = new List<Minimap.PinData>();
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
            public PinExpand(Color pinColor)
            {
                PinColor = pinColor;
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

            public void Add(Minimap.PinData pin)
            {
                m_pins.Add(pin);
            }

            public bool Remove(Minimap.PinData pin)
            {
                return m_pins.Remove(pin);
            }
        }

        private static MinimapAssistant m_instance = new MinimapAssistant();
        public static MinimapAssistant Instance { get => m_instance; private set => m_instance = value; }

        private readonly Dictionary<string, PinExpand> m_pins = new Dictionary<string, PinExpand>();
        private string m_oldPinName = string.Empty;

        private IEnumerable<Minimap.PinData> m_listUnFilteredPins;
        private Minimap.PinData m_edittingPin;

        public override void Start()
        {
            MinimapPatches.OnPinAdd += OnPinAdd;
            MinimapPatches.OnPinRemove += OnPinRemove;
            MinimapPatches.OnSpecialPinRemove += OnPinRemove;
            MinimapPatches.OnPinSetup += OnPinSetup;
            PinnaclePatches.OnSetTargetPin += OnPinSetup;
            MinimapPatches.OnPinNameChanged += OnPinUpdate;
            PinnaclePatches.OnPinNameChanged += OnPinUpdate;
        }

        public override void Destroy()
        {
            MinimapPatches.OnPinAdd -= OnPinAdd;
            MinimapPatches.OnPinRemove -= OnPinRemove;
            MinimapPatches.OnSpecialPinRemove -= OnPinRemove;
            MinimapPatches.OnPinSetup -= OnPinSetup;
            PinnaclePatches.OnSetTargetPin -= OnPinSetup;
            MinimapPatches.OnPinNameChanged -= OnPinUpdate;
            PinnaclePatches.OnPinNameChanged -= OnPinUpdate;
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

        public void ColorPins()
        {
            foreach (var kvp in m_pins)
            {
                kvp.Value.ApplyColor();
            }
        }

        public void SearchPins(string pinToFind)
        {
            ResetFilteredPins();
            // Define a regular expression pattern to match strings with double quotes at the front and back
            string pattern = "^\".*\"$";

            // Use Regex.IsMatch to check if the input matches the pattern
            bool isExact = Regex.IsMatch(pinToFind, pattern) || string.IsNullOrEmpty(pinToFind);
            if (isExact) pinToFind = pinToFind.Trim('"');

            m_listUnFilteredPins = TrackingAssistant.Instance.Pins.Values
                .Where(pinData => !CompareSearch(pinData.m_name, pinToFind, isExact));
        }

        private bool CompareSearch(string foundPin, string query, bool isExact)
        {
            foundPin = foundPin.ToLower();
            if (isExact) return foundPin.Equals(query);

            return foundPin.IndexOf(query) != -1;
        }

        public void ResetFilteredPins()
        {
            if (m_listUnFilteredPins == null) return;
            FilterPins(renderPins: true);
            m_listUnFilteredPins = null;
        }

        public void FilterPins()
        {
            if (m_listUnFilteredPins == null) return;
            FilterPins(renderPins: false);
        }

        public void FilterPins(bool renderPins)
        {
            foreach (Minimap.PinData unFilteredPin in m_listUnFilteredPins)
            {
                unFilteredPin.m_NamePinData?.PinNameGameObject?.SetActive(renderPins);
                unFilteredPin.m_uiElement?.gameObject.SetActive(renderPins);
            }
        }

        private bool InitializeKey(string key, Color pinColor, bool force = false)
        {
            if (!m_pins.TryGetValue(key, out PinExpand pinExpand))
            {
                Debug.Log($"Created colored pin group for {key}");
                m_pins.Add(key, new PinExpand(pinColor));
                return true;
            }
            Debug.Log($"Colored pin group named, '{key}' exists, won't create it");
            if (force)
            {
                Debug.Log("Changing the color only instead.");
                pinExpand.PinColor = pinColor;
                return true;
            }
            return false;
        }

        public void RelocatePin(Minimap.PinData pinToRelocate, string oldName)
        {
            string newName = pinToRelocate.m_name.ToLower();
            if (oldName.Equals(newName)) return;
            InitializeKey(newName, Color.white);

            m_pins[oldName].Remove(pinToRelocate);
            m_pins[newName].Add(pinToRelocate);
        }

        private void OnPinSetup(Minimap.PinData pin)
        {
            if (pin == null) return;
            m_edittingPin = pin;
            m_oldPinName = m_edittingPin.m_name.ToLower();
        }

        private void OnPinUpdate()
        {
            if (m_edittingPin == null) return;
            RelocatePin(m_edittingPin, m_oldPinName);
        }

        private void OnPinAdd(Minimap.PinData pin)
        {
            string newName = pin.m_name.ToLower();
            InitializeKey(newName, Color.white);
            m_pins[newName].Add(pin);
        }

        private void OnPinRemove(Minimap.PinData pin)
        {
            m_pins[pin.m_name.ToLower()]?.Remove(pin);
        }

        private void OnTrackedObjectAdd(TrackedObject trackedObject)
        {
            string pinName = trackedObject.Name.ToLower();
            Color pinColor = trackedObject.PinColor;

            Debug.Log($"Initializing pin name storage for tracked object, {trackedObject.ObjectID}.");
            InitializeKey(pinName, pinColor, true);
        }

        private void OnTrackedObjectRemove(TrackedObject trackedObject)
        {
            string pinName = trackedObject.Name.ToLower();
            if (!m_pins.ContainsKey(pinName)) return;
            Debug.Log($"Removing color for {trackedObject.ObjectID}");
            m_pins[pinName].PinColor = Color.white; // "remove" by changing the color to white
        }

        private void OnTrackedObjectUpdate(TrackedObject trackedObject, TrackedObject newTrackedObject)
        {
            string oldName = trackedObject.Name.ToLower();
            string newName = newTrackedObject.Name.ToLower();
            if (!m_pins.TryGetValue(oldName, out var pinExpand))
            {
                Jotunn.Logger.LogError("Pin not found, this is not supposed to happen!");
                return;
            }
            bool pinNameChanged = !(oldName.Equals(newName));
            OnTrackedObjectAdd(newTrackedObject);
            if (pinNameChanged) pinExpand.ResetColor();
        }

        private void OnTrackedObjectsReload(LooseDictionary<TrackedObject> trackedObjects)
        {
            foreach (var item in m_pins)
            {
                item.Value.ResetColor();
            }

            foreach (var item in trackedObjects.AltDictionary)
            {
                string pinName = item.Value.Value.Name.ToLower();
                Color pinColor = item.Value.Value.PinColor;
                InitializeKey(pinName, pinColor, force: true);
            }
            Debug.Log("Minimap Pin color reloaded");
        }
    }
}