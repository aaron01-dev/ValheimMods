using System;

namespace WxAxW.PinAssistant.Core
{
    [Serializable]
    public class TrackedObject
    {
        private string m_objectID;
        private string m_name;
        private string m_blackListWords;
        private Minimap.PinType m_icon;
        private bool m_save;
        private bool m_isChecked;
        private bool m_isExactMatchOnly;

        public TrackedObject()
        { }

        public TrackedObject(string objectID, string name, string blackListWords, Minimap.PinType icon, bool save = true, bool isChecked = false, bool isExact = false)
        {
            this.SetValues(objectID, name, blackListWords, icon, save, isChecked, isExact);
        }

        public string ObjectID { get => m_objectID; set => m_objectID = value; }
        public string Name { get => m_name; set => m_name = value; }
        public string BlackListWords { get => m_blackListWords; set => m_blackListWords = value; }
        public Minimap.PinType Icon { get => m_icon; set => m_icon = value; }
        public bool Save { get => m_save; set => m_save = value; }
        public bool IsChecked { get => m_isChecked; set => m_isChecked = value; }
        public bool IsExactMatchOnly { get => m_isExactMatchOnly; set => m_isExactMatchOnly = value; }

        public override bool Equals(object obj)
        {
            return obj is TrackedObject @object &&
                   m_objectID == @object.m_objectID;
        }

        public void SetValues(string objectID, string name, string blackListWords, Minimap.PinType icon, bool save = true, bool isChecked = false, bool isExact = false)
        {
            m_objectID = objectID;
            m_name = name;
            m_blackListWords = blackListWords;
            m_icon = icon;
            m_save = save;
            m_isChecked = isChecked;
            m_isExactMatchOnly = isExact;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_name)) return "object";
            return m_name;
        }
    }
}