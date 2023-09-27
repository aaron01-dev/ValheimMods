using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Patches;
using WxAxW.PinAssistant.Utils;
using Debug = WxAxW.PinAssistant.Utils.Debug;

namespace WxAxW.PinAssistant.Core
{
    public class PinAssistantScript
    {
        private static PinAssistantScript m_instance;
        public static PinAssistantScript Instance => m_instance ?? (m_instance = new PinAssistantScript());
        public bool isPinsInitialized = false;

        private Dictionary<Vector3, Minimap.PinData> m_pins = new Dictionary<Vector3, Minimap.PinData>();
        private static LooseDictionary<TrackedObject> m_trackedObjects;

        // a lot of things  | tins, rocks, etc      | pickables | totems,post or possible poi   | carrot seeds, etc     | minerals ores, rocks, etc | so you won't detect through walls
        // Default - 0      | Default_small - 20    | item - 12 | piece - 10                    | piece_nonsolid - 16   | static_solid - 15         | terrain - 11
        public int m_layersToCheck = LayerMask.GetMask("Default", "Default_small", "item", "piece", "piece_nonsolid", "static_solid", "terrain");

        //private Dictionary<TrackedObject, string> m_trackedObjects = new Dictionary<TrackedObject, string>();

        /*
        typeof(Destructible),   // a lot of things, objects that have other types may have this as well (ie Berry Bush - Pickable and Destructible)
        typeof(Pickable),       // things you can press to pick (flint, stone, branch, berry bush, mushroom, etc)
        typeof(MineRock),       // Giant ores' types are 'Destructible' at first because it is purely solid object with no breakable piece. When damanged, it turns into a different rock that looks exactly the same but has breakable parts and the object now has the 'MineRock' type and not 'Destructible'
        typeof(Location),       // POIs: crypts, sunken crypts, structures that transport you to a different area
        typeof(SpawnArea),      // Spawners: bone pile, etc.
        typeof(Vegvisir),       // the runestone that shows you boss locations
        typeof(ResourceRoot),   // Yggdrasil root (the giant ancient root one) at Mistlands
        typeof(TreeBase)        // Trees
        */
        public List<Type> m_trackedTypes = new List<Type>(); // tracking the components(or type in this case) from objects to determine if it's significant

        public Dictionary<Vector3, Minimap.PinData> Pins { get => m_pins; set => m_pins = value; }
        public static LooseDictionary<TrackedObject> TrackedObjects { get => m_trackedObjects; set => m_trackedObjects = value; }

        public event Action ModifiedTrackedObjects;

        public event Action LoadedTrackedObjects;

        public void Init(string serializedTrackedObjects, List<Type> registeredTypes)
        {
            m_trackedTypes.AddRange(registeredTypes);
            DeserializeTrackedObjects(serializedTrackedObjects);
            EnableClass();
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

        public void EnableClass()
        {
            isPinsInitialized = true;
            PopulatePins();
            AddAddPinListener_Patch.OnPinAdd += OnPinAdd;
            AddClearPinsListener_Patch.OnPinClear += OnPinsClear;
            AddRemovePinListener_Patch.OnPinRemove += OnPinRemove;
        }

        public void DisableClass()
        {
            isPinsInitialized = false;
            ClearPins();
            AddAddPinListener_Patch.OnPinAdd -= OnPinAdd;
            AddClearPinsListener_Patch.OnPinClear -= OnPinsClear;
            AddRemovePinListener_Patch.OnPinRemove -= OnPinRemove;
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

        public void AddType(Type type)
        {
            if (m_trackedTypes.Contains(type)) return;
            m_trackedTypes.Add(type);
        }

        public void RemoveType(Type type)
        {
            m_trackedTypes.Remove(type);
        }

        public void DeserializeTrackedObjects(string serializedObject)
        {
            m_trackedObjects = null;
            TextType message = TextType.TRACKED_OBJECTS_INITIALIZED;
            if (!string.IsNullOrEmpty(serializedObject))
            {
                try
                {
                    LooseDictionary<TrackedObject> deserializedObject = JsonConvert.DeserializeObject<LooseDictionary<TrackedObject>>(serializedObject);
                    if (deserializedObject.altDictionary.Count > 0)
                    {
                        m_trackedObjects = deserializedObject;
                        message = TextType.TRACKED_OBJECTS_LOADED;
                    }
                    else
                    {
                        message = TextType.TRACKED_OBJECTS_EMPTY;
                    }
                }
                catch (JsonException)
                {
                    message = TextType.TRACKED_OBJECTS_INVALID;
                }
            }
            if (m_trackedObjects == null) m_trackedObjects = new LooseDictionary<TrackedObject>();
            Debug.Log(message);
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

        public void Destroy()
        {
            m_instance = null;
            DisableClass();
        }
    }
}