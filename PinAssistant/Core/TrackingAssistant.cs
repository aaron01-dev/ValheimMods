using BepInEx.Configuration;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
    public class TrackingAssistant : Component
    {
        private static TrackingAssistant m_instance = new TrackingAssistant();
        public static TrackingAssistant Instance => m_instance;

        private Dictionary<Vector3, Minimap.PinData> m_pins = new Dictionary<Vector3, Minimap.PinData>();
        public Dictionary<Vector3, Minimap.PinData> Pins { get => m_pins; set => m_pins = value; }
        private LooseDictionary<TrackedObject> m_trackedObjects = new LooseDictionary<TrackedObject>();

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

        public LooseDictionary<TrackedObject> TrackedObjects { get => m_trackedObjects; set => m_trackedObjects = value; }

        public delegate string OnModifiedTrackedObjectsHandler();
        public event OnModifiedTrackedObjectsHandler OnModifiedTrackedObjects;
        public event Action<TrackedObject> OnTrackedObjectAdd;
        public event Action<TrackedObject> OnTrackedObjectRemove;
        public event Action<TrackedObject, TrackedObject> OnTrackedObjectUpdate;

        public event Action OnTrackedObjectSaved;
        public event Action<LooseDictionary<TrackedObject>> OnTrackedObjectsReload;

        public override void Start()
        {

        }

        public override void OnEnable()
        {
            foreach (var kvp in ModConfig.Instance.TypeDictionary)
            {
                // check if type is allowed to be found
                if (kvp.Key.Value) m_trackedTypes.Add(kvp.Value);
            }
            DeserializeTrackedObjects(ModConfig.Instance.TrackedObjectsConfig.Value);

            foreach (ConfigEntry<bool> entry in ModConfig.Instance.TypeDictionary.Keys)
            {
                entry.SettingChanged += OnToggleTypeConfig;
            }
            MinimapPatches.OnPinAdd += OnPinAdd;
            MinimapPatches.OnPinClear += OnPinsClear;
            MinimapPatches.OnPinRemove += OnPinRemove;
            OnModifiedTrackedObjects += SerializeTrackedObjects;
            PopulatePins();
        }

        public override void OnDisable()
        {
            m_trackedTypes.Clear();
            m_trackedObjects.Clear();
            foreach (ConfigEntry<bool> entry in ModConfig.Instance.TypeDictionary.Keys)
            {
                entry.SettingChanged -= OnToggleTypeConfig;
            }
            MinimapPatches.OnPinAdd -= OnPinAdd;
            MinimapPatches.OnPinClear -= OnPinsClear;
            MinimapPatches.OnPinRemove -= OnPinRemove;
            OnModifiedTrackedObjects -= SerializeTrackedObjects;
            ClearPins();
        }

        public override void Destroy()
        {
            m_instance = null;
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
                UnityEngine.Component objComponent = hit.collider.GetComponentInParent(type, true);
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
            List<Minimap.PinData> MapPins = Minimap.instance.m_pins;

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

        public bool AddTrackedObject(TrackedObject trackedObjectToAdd, out bool conflicting)
        {
            string key = trackedObjectToAdd.ObjectID;
            bool nodeExact = trackedObjectToAdd.IsExactMatchOnly;
            string blackListWords = trackedObjectToAdd.BlackListWords;
            bool success = m_trackedObjects.Add(key, trackedObjectToAdd, out conflicting, blackListWords, nodeExact);
            if (!success) return false;

            Debug.Log($"Tracking {trackedObjectToAdd.ObjectID}");
            OnTrackedObjectAdd?.Invoke(trackedObjectToAdd);
            OnModifiedTrackedObjects?.Invoke();
            return true;
        }

        public bool RemoveTrackedObject(TrackedObject trackedObjectToDelete)
        {
            bool success = m_trackedObjects.Remove(trackedObjectToDelete.ObjectID);
            if (!success) return false;

            Debug.Log($"Untracking {trackedObjectToDelete.ObjectID}");
            OnTrackedObjectRemove?.Invoke(trackedObjectToDelete);
            OnModifiedTrackedObjects?.Invoke();
            return true;
        }

        public bool ModifyTrackedObject(TrackedObject objToEdit, TrackedObject newValues, out bool conficting, out TrackedObject foundConflict)
        {
            // attempt to change key
            
            m_trackedObjects.ChangeKey(
                key: objToEdit.ObjectID, 
                newKey: newValues.ObjectID, 
                out conficting, 
                out foundConflict
                );

            // modify values
            bool success = m_trackedObjects.Modify(
                key: newValues.ObjectID,
                newValues,
                newNodeExact: newValues.IsExactMatchOnly,
                newBlackListWord: newValues.BlackListWords
                );
            if (!success)
            {
                Debug.Warning("Tracked object not found");
                return false;
            }
            OnTrackedObjectUpdate?.Invoke(objToEdit, newValues);
            OnModifiedTrackedObjects?.Invoke();
            return true;
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

        public string SerializeTrackedObjects()
        {
            Debug.Log("Serializing tracked objects");
            string json = JsonConvert.SerializeObject(
                m_trackedObjects, Formatting.Indented,
                new JsonSerializerSettings() {
                    FloatParseHandling = FloatParseHandling.Decimal,        // to prevent crashing when parsing colors which has more than 2 decimal places
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,   // to prevent exception when serializing color to prevent serializing reference loop like static colors
                });
            Debug.Log("Serialized, saving to data");
            ModConfig.Instance.TrackedObjectsConfig.Value = json;
            Debug.Log("Saved");
            OnTrackedObjectSaved?.Invoke();
            return json;
        }

        public void DeserializeTrackedObjects(string serializedObject)
        {
            m_trackedObjects.Clear();
            TextType message = TextType.TRACKED_OBJECTS_INITIALIZED;
            if (!string.IsNullOrEmpty(serializedObject))
            {
                try
                {
                    LooseDictionary<TrackedObject> deserializedObject = JsonConvert.DeserializeObject<LooseDictionary<TrackedObject>>(
                        serializedObject,
                        new JsonSerializerSettings(){
                            ObjectCreationHandling = ObjectCreationHandling.Replace // to trigger setters to avoid making a code to initialize deserialized objects
                        });

                    if (deserializedObject.AltDictionary.Count > 0)
                    {
                        m_trackedObjects = deserializedObject;
                        message = TextType.TRACKED_OBJECTS_LOADED;
                    }
                    else
                    {
                        message = TextType.TRACKED_OBJECTS_EMPTY;
                    }
                }
                catch (JsonException e)
                {
                    Debug.Log(e);
                    message = TextType.TRACKED_OBJECTS_INVALID;
                }
            }
            Debug.Log(message);
            OnTrackedObjectsReload?.Invoke(m_trackedObjects);
        }

        private void OnPinAdd(Minimap.PinData pinData)
        {
            /*
            Debug.Log(TextType.PIN_ADDING, "OnPinAdd", pinData.m_name, pinData.m_pos);
            if (pinData.m_pos == Vector3.zero)
            {
                Debug.Log(TextType.PING_ADDING);
                return;
            }
            if (m_pins.ContainsKey(pinData.m_pos))
            {
                Debug.Log(TextType.PIN_ADDING_EXISTS);
                return;
            }
            */
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

        [Obsolete]
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
            TrackedObject newTrackedObject = new TrackedObject(formattedName, formattedName, "", Minimap.PinType.Death, Color.white, true, true, false);
            AddTrackedObject(newTrackedObject, out _);
        }

        private void OnToggleTypeConfig(object sender, EventArgs _)
        {
            ConfigEntry<bool> currTypeConfig = (ConfigEntry<bool>)sender;
            Type currType = ModConfig.Instance.TypeDictionary[currTypeConfig];
            bool value = currTypeConfig.Value;
            if (value) AddType(currType);
            else RemoveType(currType);
        }
    }
}