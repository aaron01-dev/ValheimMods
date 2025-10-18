using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Core
{
    internal class PinGroup
    {
        private readonly List<Minimap.PinData> m_pins = new List<Minimap.PinData>();
        private string m_pinName = string.Empty;
        private Minimap.PinType m_pinType = Minimap.PinType.None;
        private Color m_pinColor;
        private Color m_pinColorShared; // Just to store the shared color variant to avoid constant maths every frame

        public List<Minimap.PinData> Pins { get => m_pins; }

        public string PinName { get => m_pinName; }

        public Minimap.PinType PinType { get => m_pinType; }

        public Color PinColor
        {
            get => m_pinColor;
            set
            {
                m_pinColor = value;
                m_pinColorShared = new Color(value.r * 0.7f, value.g * 0.7f, value.b * 0.7f, value.a * 0.8f);
                ApplyColor(); // Apply color change immediately to all pins in the group
            }
        }

        public PinGroup(string pinName, Minimap.PinType pinType, Color pinColor)
        {
            SetValues(pinName, pinType, pinColor);
        }

        ~PinGroup()
        {
            Debug.Log($"PinGroup, {m_pinName}, {m_pinType}, Destroyed!");
        }

        public void TransferTo(PinGroup newPinGroup)
        {
            // No 2 pin group can have the same pins, so we need to remove all pins from the other group and add them to the current group
            List<Minimap.PinData> retrievedPins = RemoveAll();
            newPinGroup.AddRange(retrievedPins);
            Clear();
            Debug.Log($"Transferred, {m_pinName} | {m_pinType} to {newPinGroup.m_pinName} | {newPinGroup.m_pinType}");
        }

        public void ApplyColor()
        {
            if (PinColor == Color.white) return;
            Color pinFadeColor = m_pinColorShared;
            pinFadeColor.a *= Minimap.instance.m_sharedMapDataFade;
            foreach (var pin in m_pins)
            {
                Image currPinIcon = pin.m_iconElement;
                if (currPinIcon == null) continue;

                currPinIcon.color = pin.m_ownerID == 0 ? PinColor : pinFadeColor;
            }
        }

        public void ResetColor()
        {
            PinColor = Color.white;
        }

        public void ModifyPinGroupPins(string newName, Minimap.PinType newType)
        {
            if (!m_pinName.Equals(newName))
            {
                m_pinName = newName;
                foreach (var pin in m_pins)
                {
                    PinHandler.SetPinName(pin, m_pinName);
                }
            }
            if (m_pinType != newType)
            {
                m_pinType = newType;
                foreach (var pin in m_pins)
                {
                    PinHandler.SetPinType(pin, m_pinType);
                }
            }
        }

        public void Add(Minimap.PinData pin)
        {
            m_pins.Add(pin);
        }

        public void AddRange(List<Minimap.PinData> pins)
        {
            PinHandler.SetPinsData(pins, m_pinName, m_pinType);
            ApplyColor(); // Apply color to newly added pins to immediately show in UI
            m_pins.AddRange(pins);
        }

        public bool Remove(Minimap.PinData pin)
        {
            if (m_pins.Remove(pin))
            {
                Debug.Log($"Removed pin from group, {m_pinName}");
                return true;
            }
            return false;
        }

        public List<Minimap.PinData> RemoveAll()
        {
            List<Minimap.PinData> removedPins = new List<Minimap.PinData>(m_pins);
            Clear();
            return removedPins;
        }

        public void Clear()
        {
            m_pins.Clear();
        }

        public void SetValues(string pinName, Minimap.PinType pinType, Color pinColor)
        {
            m_pinName = pinName;
            m_pinType = pinType;
            PinColor = pinColor;
        }
    }

}
