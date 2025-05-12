using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Jotunn.Managers;
using UnityEngine;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Patches;
using WxAxW.PinAssistant.Utils;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Core
{
    internal class PinHandler : PluginComponent
    {
        private static PinHandler m_instance = new PinHandler();

        // Pins that are not special (boss pin, shop, etc.) for better lookup instead of iterating. Keys are based on pin position (cause it's unlikely to pin at the exact same spot)
        private Dictionary<Vector3, Minimap.PinData> m_monitoredPins = new Dictionary<Vector3, Minimap.PinData>();

        private Dictionary<Minimap.PinType, Tuple<Sprite, string>> m_dictionaryPinType = new Dictionary<Minimap.PinType, Tuple<Sprite, string>>();
        private bool m_dictionaryPinTypePopulated = false;
        public Action OnDictionaryPinTypePopulated;

        public static PinHandler Instance { get => m_instance; set => m_instance = value; }
        public Dictionary<Vector3, Minimap.PinData> MonitoredPins { get => m_monitoredPins; set => m_monitoredPins = value; }
        public Dictionary<Minimap.PinType, Tuple<Sprite, string>> DictionaryPinType { get => m_dictionaryPinType; set => m_dictionaryPinType = value; }
        public bool DictionaryPinTypePopulated { get => m_dictionaryPinTypePopulated; set => m_dictionaryPinTypePopulated = value; }

        public override void Start()
        {
            MinimapManager.OnVanillaMapAvailable += PopulateIcons;
        }
        
        public override void Destroy() {}

        public override void OnEnable()
        {
            MinimapPatches.OnPinAdd += OnPinAdd;
            MinimapPatches.OnPinRemove += OnPinRemove;
            MinimapPatches.OnPinUpdate += OnPinUpdate;
            MinimapPatches.OnPinClear += OnPinsClear;
            PopulatePins();

        }

        public override void OnDisable()
        {
            MinimapPatches.OnPinAdd -= OnPinAdd;
            MinimapPatches.OnPinRemove -= OnPinRemove;
            MinimapPatches.OnPinUpdate -= OnPinUpdate;
            MinimapPatches.OnPinClear -= OnPinsClear;
            ClearPins(); // TODO: Delete this when a feature for single module enable/disable is added, cause CheckPinPositionExists rely on m_monitoredPins.
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

        // only happens when loading to in game or re-enabling mod
        private void PopulatePins()
        {
            if (Minimap.instance == null && !Plugin.Instance.m_isInGame) { Debug.Warning(TextType.MINIMAP_NOT_FOUND); return; }
            // populate pins with valheim pins
            List<Minimap.PinData> MapPins = Minimap.instance.m_pins;

            if (MapPins == null || MapPins.Count == 0) { Debug.Log(TextType.WORLD_LOADING); return; }
            foreach (Minimap.PinData pin in MapPins)
            {
                Debug.Log(TextType.PIN_ADDING, "PopulatePins", pin.m_name, pin.m_pos);
                if (m_monitoredPins.ContainsKey(pin.m_pos)) continue;
                m_monitoredPins.Add(pin.m_pos, pin);
            }
            Debug.Log(TextType.PINS_POPULATED);
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
            m_dictionaryPinTypePopulated = true;
            OnDictionaryPinTypePopulated?.Invoke();

            MinimapManager.OnVanillaMapAvailable -= PopulateIcons; // unsubscribe
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

        public void AddPin(Vector3 pos, string name, Minimap.PinType icon, bool save, bool isChecked)
        {
            if (CheckPinPositionExist(pos))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS);
                return;
            }

            float redundancyDistanceAny = ModConfig.Instance.RedundancyDistanceAnyConfig.Value;
            if (redundancyDistanceAny != 0f && !CheckValidPinPosition(pos, name, redundancyDistanceAny, allPins: true))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS_NEARBY);
                return;
            }
            
            float redundancyDistanceSame = ModConfig.Instance.RedundancyDistanceSameConfig.Value;
            if (redundancyDistanceSame != 0f && !CheckValidPinPosition(pos, name, redundancyDistanceSame, allPins: false))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS_SIMILAR_NEARBY);
                return;
            }

            Minimap.instance.AddPin(pos, icon, name, save, isChecked);
        }

        private void RemovePin(Minimap.PinData pinData)
        {
            Vector3 key = pinData.m_pos;
            if (!m_monitoredPins.TryGetValue(key, out var pin)) return;
            if (pin != pinData) return;
            m_monitoredPins.Remove(key);
            Debug.Log(TextType.PIN_REMOVED, pinData.m_name, pinData.m_pos);
        }

        private void ClearPins()
        {
            m_monitoredPins.Clear();
            Debug.Log(TextType.PINS_CLEARED);
        }

        // Used for easy checking of existing pins instead of iterating through the list of pins
        private bool CheckPinPositionExist(Vector3 pinPos)
        {
            return m_monitoredPins.ContainsKey(pinPos);
        }

        // TODO: use pin group
        internal bool CheckValidPinPosition(Vector3 pinToAdd, string pinName, float redundancyDistance, bool allPins)
        {
            foreach (Minimap.PinData pin in m_monitoredPins.Values)
            {
                if (!allPins)
                {
                    bool diffName = pin.m_name.IndexOf(pinName, StringComparison.OrdinalIgnoreCase) == -1;
                    if (diffName) continue;
                }
                bool valid = CheckValidDistance(pinToAdd, pin.m_pos, redundancyDistance);
                if (valid) continue;
                return false;
            }
            return true;
        }

        private bool CheckValidDistance(Vector3 v1, Vector3 v2, float redundancyDistance)
        {
            return Get2DDistance(v1, v2) > redundancyDistance;
        }

        private float Get2DDistance(Vector3 v1, Vector3 v2)
        {
            // (x_2 - x_1)
            return Mathf.Sqrt(Mathf.Pow((v2.x - v1.x), 2f) + Mathf.Pow((v2.z - v1.z), 2f));
        }

        private void OnPinAdd(Minimap.PinData pinData)
        {
            Debug.Log(TextType.PIN_ADDING, "OnPinAdd", pinData.m_name, pinData.m_pos);
            if (MinimapPatches.isSpecialPin)
            {
                Debug.Log("Special Pin found will not include in the list of pins in PinAssistant");
                return;
            }
            if (pinData.m_pos == Vector3.zero)
            {
                Debug.Log(TextType.PING_ADDING);
                return;
            }
            if (m_monitoredPins.ContainsKey(pinData.m_pos))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS);
                return;
            }
            Debug.Log(TextType.PIN_ADDED);
            m_monitoredPins.Add(pinData.m_pos, pinData);
        }

        private void OnPinRemove(Minimap.PinData pinData)
        {
            RemovePin(pinData);
        }

        private void OnPinUpdate()
        {
            Minimap.PinData oldPin = MinimapPatches.m_edittingPinInitial;
            Minimap.PinData newPin = MinimapPatches.m_edittingPin;
            if (oldPin.m_pos == newPin.m_pos) return;
            m_monitoredPins.ChangeKey(oldPin.m_pos, newPin.m_pos);
        }
        private void OnPinsClear()
        {
            m_monitoredPins.Clear();
        }
    }
}
