using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using WxAxW.PinAssistant.Patches;
using WxAxW.PinAssistant.Utils;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Components
{
    public class PinAssistant
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

        private static PinAssistant m_instance;
        public static PinAssistant Instance => m_instance ?? (m_instance = new PinAssistant());
        public bool isPinsInitialized = false;

        private Dictionary<Vector3, Minimap.PinData> m_pins = new Dictionary<Vector3, Minimap.PinData>();
        private static LooseDictionary<TrackedObject> m_trackedObjects;

        // a lot of things  | tins, rocks, etc      | pickables | totems,post or possible poi   | carrot seeds, etc     | minerals ores, rocks, etc | so you won't detect through walls
        // Default - 0      | Default_small - 20    | item - 12 | piece - 10                    | piece_nonsolid - 16   | static_solid - 15         | terrain - 11
        public int m_layersToCheck = LayerMask.GetMask("Default", "Default_small", "item", "piece", "piece_nonsolid", "static_solid", "terrain");

        //private Dictionary<TrackedObject, string> m_trackedObjects = new Dictionary<TrackedObject, string>();

        public Type[] m_trackedTypes = { // tracking the components(or type in this case) from objects to determine if it's significant
            typeof(Destructible),   // a lot of things, objects that have other types may have this as well (ie Berry Bush - Pickable and Destructible)
            typeof(Pickable),       // things you can press to pick (flint, stone, branch, berry bush, mushroom, etc)
            typeof(MineRock),       // Minerals (Note. ores starts at Destructible because it is purely solid object with no breakable parts, its as then when damanged, it turns into a different rock which as breakable parts and the object now has the component(or type) MineRock and not Destructible
            typeof(Location),       // POIs crypts, sunken crypts, structures that transport you to a different map
            typeof(SpawnArea),      // Spawners, bone pile, etc.
            typeof(Vegvisir),       // the runestone that shows you boss locations
            typeof(ResourceRoot)    // Yggdrasil root (the giant ancient root one)
        };

        public Dictionary<Vector3, Minimap.PinData> Pins { get => m_pins; set => m_pins = value; }
        public static LooseDictionary<TrackedObject> TrackedObjects { get => m_trackedObjects; set => m_trackedObjects = value; }

        public delegate void ModifiedTrackedObjectsHandler();

        public event ModifiedTrackedObjectsHandler ModifiedTrackedObjects;

        public delegate void LoadedTrackedObjectsHandler();

        public event LoadedTrackedObjectsHandler LoadedTrackedObjects;

        public void Init(string serializedTrackedObjects)
        {
            DeserializeTrackedObjects(serializedTrackedObjects);
        }

        public void PinLookedObject(float lookDistance, float redundancyDistance)
        {
            GameObject obj = LookAt(lookDistance);
            if (obj != null) AddObjAsPin(obj, redundancyDistance);
        }

        public GameObject LookAt(float lookDistance)
        {
            if (GameCamera.instance == null) return null;

            // create a ray to check for objects output to hit var
            Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward,
            out var hit, lookDistance, m_layersToCheck);

            if (hit.collider == null) return null; // check if looking at no object
            if (hit.collider.gameObject.layer == 11) return null; // skip if terrain
            foreach (var type in m_trackedTypes)
            {
                Component objComponent = hit.collider.GetComponentInParent(type, true);
                if (objComponent == null) continue;
                GameObject obj = objComponent.gameObject;
                Debug.Log(TextType.OBJECT_INFO, obj.name, LayerMask.LayerToName(obj.layer), obj.layer, objComponent.GetType());

                return obj; // Pinnable object found
            }
            return null;
        }

        public void AddObjAsPin(GameObject obj, float redundancyDistance)
        {
            if (Minimap.instance is null) return;
            bool exist = m_trackedObjects.TryGetValueLooseLite(obj.name, out TrackedObject trackedObject);
            // checks for if it's valid to pin this object
            if (!exist)
            {
                Debug.Log(TextType.OBJECT_NOT_TRACKED);
                return;
            }
            if (CheckPinPositionExist(obj.transform))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS);
                return;
            }
            if (!CheckValidPinPosition(obj.transform.position, trackedObject.Name, redundancyDistance))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS_NEARBY);
                return;
            }
            Minimap.instance.AddPin(obj.transform.position, trackedObject.Icon, trackedObject.Name, trackedObject.Save, trackedObject.IsChecked);
        }

        private bool CheckPinPositionExist(Transform obj)
        {
            // check plugin created dictionary for pinData player pin data from valheim to easily check for existing by using vector position as key
            m_pins.TryGetValue(obj.position, out var pin);
            return pin != null;
        }

        public string FormatObjectName(string name)
        {
            // Remove alphanumeric values ex. "SunkenCrypt4" -> "SunkenCrypt"
            name = Regex.Replace(name, @"\d", string.Empty);

            // Replace '_' to ' '
            name = Regex.Replace(name, "_", " ");

            // Pickable_small_rock
            //
            // Remove specific words or alphanumeric values ex. "Sunken_Crypt4(Clone)" -> "SunkenCrypt4"
            name = RemoveWords(name, "(Clone)", "Pickable", "small", "rock");

            // Split conjoined words ex. "SunkenCrypt" -> "Sunken Crypt"
            name = SplitConjoinedWords(name);

            return name;
        }

        private string RemoveWords(string input, params string[] wordsToRemove)
        {
            foreach (var word in wordsToRemove)
            {
                string newInput = Regex.Replace(input, Regex.Escape(word), string.Empty, RegexOptions.IgnoreCase).Trim();
                if (string.IsNullOrEmpty(newInput)) continue;   // fail safe if formatted is empty
                input = newInput;
            }

            return input;
        }

        private string SplitConjoinedWords(string input)
        {
            // Use a regular expression to find conjoined words (e.g., BerryBush) and add a space in between.
            input = Regex.Replace(input, @"([a-z])([A-Z])", "$1 $2");
            return input;
        }

        private bool CheckValidPinPosition(Vector3 pinToAdd, string pinName, float redundancyDistance)
        {
            foreach (Minimap.PinData pin in m_pins.Values)
            {
                if (!pin.m_name.Equals(pinName)) continue;
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

        // only happens when loading to in game or re-enabling mod
        private void PopulatePins()
        {
            if (Minimap.instance == null) { Debug.Log(TextType.MINIMAP_NOT_FOUND); return; }
            // populate pins with valheim pins
            List<Minimap.PinData> MapPins = Traverse.Create(Minimap.instance)?.Field("m_pins").GetValue<List<Minimap.PinData>>();

            if (MapPins == null || MapPins.Count == 0) { Debug.Log(TextType.WORLD_LOADING); return; }
            foreach (Minimap.PinData pin in MapPins)
            {
                Debug.Log(TextType.PIN_ADDING, "PopulatePins", pin.m_name, pin.m_pos);
                if (m_pins.ContainsKey(pin.m_pos)) continue;
                m_pins.Add(pin.m_pos, pin);
            }
            Debug.Log(TextType.PINS_POPULATED);
        }

        private void RemovePin(Minimap.PinData pinData)
        {
            if (m_pins.Remove(pinData.m_pos)) Debug.Log(TextType.PIN_REMOVED, pinData.m_name, pinData.m_pos);
        }

        private void ClearPins()
        {
            m_pins.Clear();
            Debug.Log(TextType.PINS_CLEARED);
        }

        private void OnPinAdd(Minimap.PinData pinData)
        {
            Debug.Log(TextType.PIN_ADDING, "OnPinAdd", pinData.m_name, pinData.m_pos);
            if (m_pins.ContainsKey(pinData.m_pos))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS);
                return;
            }
            if (pinData.m_pos == Vector3.zero)
            {
                Debug.Log(TextType.PING_ADDING);
                return;
            }
            Debug.Log(TextType.PIN_ADDED);
            m_pins.Add(pinData.m_pos, pinData);
        }

        private void OnPinRemove(Minimap.PinData pinData)
        {
            RemovePin(pinData);
        }

        private void OnPinsClear()
        {
            m_pins.Clear();
        }

        public void DisableClass()
        {
            isPinsInitialized = false;
            ClearPins();
            AddAddPinListener_Patch.OnPinAdd -= OnPinAdd;
            AddClearPinsListener_Patch.OnPinClear -= OnPinsClear;
            AddRemovePinListener_Patch.OnPinRemove -= OnPinRemove;
        }

        public void EnableClass()
        {
            isPinsInitialized = true;
            PopulatePins();
            AddAddPinListener_Patch.OnPinAdd += OnPinAdd;
            AddClearPinsListener_Patch.OnPinClear += OnPinsClear;
            AddRemovePinListener_Patch.OnPinRemove += OnPinRemove;
        }

        public bool AddTrackedObject(string objectID, TrackedObject newTrackedObject, out bool conflicting, string blackListedWords = "", bool isExactMatchOnly = false)
        {
            bool success = m_trackedObjects.Add(objectID, newTrackedObject, out conflicting, blackListedWords, isExactMatchOnly);
            ModifiedTrackedObjects?.Invoke();
            return success;
        }

        public bool RemoveTrackedObject(string objectID, bool isExactMatchOnly = false)
        {
            bool success = m_trackedObjects.Remove(objectID, isExactMatchOnly);
            ModifiedTrackedObjects?.Invoke();
            return success;
        }

        public void DeserializeTrackedObjects(string serializedObject)
        {
            if (string.IsNullOrEmpty(serializedObject))
            {
                m_trackedObjects = new LooseDictionary<TrackedObject>();
                Debug.Log(TextType.TRACKED_OBJECTS_INITIALIZED);
            }
            else
            {
                m_trackedObjects = JsonConvert.DeserializeObject<LooseDictionary<TrackedObject>>(serializedObject);
                Debug.Log(TextType.TRACKED_OBJECTS_LOADED);
            }
            LoadedTrackedObjects?.Invoke();
        }

        public string SerializeTrackedObjects()
        {
            return JsonConvert.SerializeObject(m_trackedObjects);
        }

        // obsolete
        public void TrackLookedObjectToAutoPin(float lookDistance)
        {
            GameObject obj = LookAt(lookDistance);
            if (obj == null) return;
            string formattedName = FormatObjectName(obj.name);
            if (m_trackedObjects.TryGetValueLoose(formattedName, out _))
            {
                Debug.Log(TextType.TRACK_FAIL, formattedName);
                return; // if key exist
            }
            Debug.Log(TextType.TRACK_SUCCESS, formattedName);
            TrackedObject newTrackedObject = new TrackedObject(formattedName, formattedName, "", Minimap.PinType.Death, true, true, false);
            AddTrackedObject(formattedName, newTrackedObject, out _);
        }
    }
}