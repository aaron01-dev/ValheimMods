using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Core
{
    [Serializable]
    public class TrackedObject
    {
        public class SerializableColor
        {
            public float r = 1f;
            public float g = 1f;
            public float b = 1f;
            public float a = 1f;

            public SerializableColor() { }
            public SerializableColor(Color color) {
                SetWithColor(color);
            }
            public SerializableColor(float r, float g, float b, float a) {
                this.r = r;
                this.g = g;
                this.b = b;
                this.a = a;
            }

            public void SetWithColor(Color color)
            {
                r = color.r;
                g = color.g;
                b = color.b;
                a = color.a;
            }

            internal void ConvertToColor(ref Color pinColor)
            {
                pinColor.r = r;
                pinColor.g = g;
                pinColor.b = b;
                pinColor.a = a;
            }
        }

        private string m_objectID;
        private string m_name;
        private string m_blackListWords;
        private Minimap.PinType m_icon;
        private Color m_pinColor = Color.white;
        private bool m_save;
        private bool m_isChecked;
        private bool m_isExactMatchOnly;

        private SerializableColor m_serializeColor = new SerializableColor();

        // private SerializableColor m_serializableColor;
        public TrackedObject()
        {
        }

        public TrackedObject(string objectID, string name, string blackListWords, Minimap.PinType icon, Color pinColor, bool save = true, bool isChecked = false, bool isExact = false)
        {
            SetValues(objectID, name, blackListWords, icon, pinColor, save, isChecked, isExact);
        }

        public string ObjectID { get => m_objectID; set => m_objectID = value; }
        public string Name { get => m_name; set => m_name = value; }
        public string BlackListWords { get => m_blackListWords; set => m_blackListWords = value; }
        public Minimap.PinType Icon { get => m_icon; set => m_icon = value; }
        [JsonIgnore]
        public Color PinColor { get => m_pinColor; set => m_pinColor = value; }
        public bool Save { get => m_save; set => m_save = value; }
        public bool IsChecked { get => m_isChecked; set => m_isChecked = value; }
        public bool IsExactMatchOnly { get => m_isExactMatchOnly; set => m_isExactMatchOnly = value; }

        public SerializableColor SerializeColor
        {
            get => m_serializeColor;
            set
            {
                m_serializeColor = value;
                m_serializeColor.ConvertToColor(ref m_pinColor);
            }
        }

        public void SetValues(TrackedObject newValues)
        {
            SetValues(newValues.ObjectID, newValues.Name, newValues.BlackListWords, newValues.Icon, newValues.PinColor, newValues.Save, newValues.IsChecked, newValues.IsExactMatchOnly);
        }

        public void SetValues(string objectID, string name, string blackListWords, Minimap.PinType icon, Color pinColor, bool save = true, bool isChecked = false, bool isExact = false)
        {
            m_objectID = objectID;
            m_name = name;
            m_blackListWords = blackListWords;
            m_icon = icon;
            if (m_serializeColor == null) m_serializeColor = new SerializableColor(pinColor);
            else m_serializeColor.SetWithColor(pinColor);
            m_pinColor = pinColor;
            m_save = save;
            m_isChecked = isChecked;
            m_isExactMatchOnly = isExact;
        }

        public int GetPinIntAsDropdown()
        {
            int actualPinType = (int)this.Icon;

            if (actualPinType >= (int)Minimap.PinType.None)
                return actualPinType -1;
            else return actualPinType;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_name)) return "object";
            return m_name;
        }

        public override bool Equals(object obj)
        {
            return obj is TrackedObject @object &&
                   m_objectID == @object.m_objectID;
        }

        public override int GetHashCode()
        {
            return 1328540911 + EqualityComparer<string>.Default.GetHashCode(m_objectID);
        }
    }
}