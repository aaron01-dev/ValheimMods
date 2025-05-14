﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace WxAxW.PinAssistant.Core
{
    internal class PinGroup
    {
        private readonly List<Minimap.PinData> m_pins = new List<Minimap.PinData>();
        private string m_pinName = string.Empty;
        private Minimap.PinType m_pinType = Minimap.PinType.None;
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
                Sprite pinSprite = Minimap.instance.GetSprite(m_pinType);
                foreach (var pin in m_pins)
                {
                    PinHandler.SetPinType(pin, m_pinType, pinSprite);
                }
            }
        }

        public void Add(Minimap.PinData pin)
        {
            m_pins.Add(pin);
        }

        public void AddRange(PinGroup pinGroup)
        {
            pinGroup.ModifyPins(m_pinName, m_pinType);
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
            m_pinName = pinName;
            m_pinType = pinType;
            PinColor = pinColor;
        }
    }

}
