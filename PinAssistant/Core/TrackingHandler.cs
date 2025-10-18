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
    public class TrackingHandler : PluginComponent
    {
        private static TrackingHandler m_instance = new TrackingHandler();
        public static TrackingHandler Instance => m_instance;

        private LooseDictionary<TrackedObject> m_trackedObjects = new LooseDictionary<TrackedObject>();

        // a lot of things  | tins, rocks, etc      | pickables | totems,post or possible poi   | carrot seeds, etc     | minerals ores, rocks, etc | so you won't detect through walls
        // Default - 0      | Default_small - 20    | item - 12 | piece - 10                    | piece_nonsolid - 16   | static_solid - 15         | terrain - 11
        public int m_layersToCheck = LayerMask.GetMask(
            "Default",          // 0 - a lot of things
            "Default_small",    // 20 - tins, rocks, etc
            "item",             // 12 - pickables
            "piece",            // 10 - totems, post or possible poi
            "piece_nonsolid",   // 16 - carrot seeds, etc
            "static_solid",     // 15 - minerals ores, rocks, etc
            "terrain"           // 11 - so you won't detect through walls
            );

        /*
        Check ModConfig for different types
        */
        public List<Type> m_trackedTypes = new List<Type>(); // tracking the components(or type in this case) from objects to determine if it's significant

        public LooseDictionary<TrackedObject> TrackedObjects { get => m_trackedObjects; set => m_trackedObjects = value; }

        public delegate string OnModifiedTrackedObjectsHandler();

        public event OnModifiedTrackedObjectsHandler OnModifiedTrackedObjects;

        public event Action<TrackedObject> OnTrackedObjectAdd;

        public event Action<TrackedObject> OnTrackedObjectRemove;

        public event Action<TrackedObject, TrackedObject, bool> OnTrackedObjectUpdate;

        public event Action<LooseDictionary<TrackedObject>> OnTrackedObjectSaved;

        public event Action<LooseDictionary<TrackedObject>> OnTrackedObjectsReload;

        public override void Start()
        {
        }

        public override void OnEnable()
        {
            DeserializeTrackedObjects(ModConfig.Instance.TrackedObjectsConfig.Value);
            OnModifiedTrackedObjects += SerializeTrackedObjects;
        }

        public override void OnDisable()
        {
            m_trackedTypes.Clear();
            m_trackedObjects.Clear();
            OnModifiedTrackedObjects -= SerializeTrackedObjects;
        }

        public override void Destroy()
        {
            m_instance = null;
        }

        public void PinLookedObject(float lookDistance)
        {
            bool foundObject = LookAt(lookDistance, out string id, out GameObject obj);
            if (!foundObject) return;
            
            AddObjAsPin(id, obj);
        }

        public bool LookAt(float lookDistance, out string id, out GameObject obj)
        {
            id = string.Empty;
            obj = null;
            if (GameCamera.instance == null) return false;

            // create a ray to check for objects output to hit var
            if (!Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward,
            out var hit, lookDistance, m_layersToCheck))
                return false; // not collision

            if (hit.transform.gameObject.layer == 11) return false; // skip if terrain
            obj = hit.transform.root.gameObject;
            id = ProcessLookedObjectName(obj);
            Debug.Log(TextType.OBJECT_INFO, id, LayerMask.LayerToName(obj.layer), obj.layer);
            return true;
        }

        private string ProcessLookedObjectName(GameObject obj)
        {
            switch (obj.name)
            {
                case "LocationProxy(Clone)": // root's name is location proxy, is too amiguous (could be troll cave, crypt, sunken crypt, and even runestone
                    return obj.transform.GetChild(0).name; // so get the child component with the actual name
                case "___MineRock5 m_meshFilter":
                    return "Invalid! Track undamaged instead";

                // invalidate dungeon interior structures
                case "DG_ForestCrypt(Clone)":
                case "DG_SunkenCrypt(Clone)":
                case "TreasureChest_forestcrypt(Clone)":
                case "TreasureChest_sunkencrypt(Clone)":
                case "Pickable_ForestCryptRemains01(Clone)":
                case "Pickable_ForestCryptRemains02(Clone)":
                case "Pickable_ForestCryptRemains03(Clone)":
                case "Pickable_ForestCryptRandom(Clone)":
                case "Pickable_SunkenCryptRandom(Clone)":
                case "dungeon_forestcrypt_door(Clone)":
                case "sunken_crypt_gate(Clone)":
                    return "";
                default:
                    return obj.name;
            }
        }

        public void AddObjAsPin(string id, GameObject obj)
        {
            if (Minimap.instance is null) return;
            bool exist = m_trackedObjects.TryGetValueLooseLite(id, out TrackedObject trackedObject);
            // checks for if it's valid to pin this object
            if (!exist)
            {
                Debug.Log(TextType.OBJECT_NOT_TRACKED);
                return;
            }
            Vector3 pos = obj.transform.position;

            PinHandler.Instance.AddPin(pos, trackedObject.Name, trackedObject.Icon, trackedObject.Save, trackedObject.IsChecked);
        }

        public string FormatObjectName(string name)
        {

            // Replace '_' to ' '
            name = Regex.Replace(name, "_", " ");

            // Pickable_small_rock4
            //
            // Remove alphanumeric values ex. "SunkenCrypt4" -> "SunkenCrypt"
            // Remove specific words ex. rock
            name = Regex.Replace(name, @"(?:\d|\(clone\))|\b(?:pickable|small)\b", string.Empty, RegexOptions.IgnoreCase);
            string noRock = Regex.Replace(name, @"rock", string.Empty, RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(noRock)) name = noRock;   // fail safe if formatted is empty

            // Capitalize Words
            name = Regex.Replace(name, @"\b\w", m => m.Value.ToUpper());

            // Split conjoined words ex. "SunkenCrypt" -> "Sunken Crypt"
            name = Regex.Replace(name, @"([a-z])([A-Z])", "$1 $2").Trim();

            return name;
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

        public bool ModifyTrackedObject(TrackedObject objToEdit, TrackedObject newValues, bool renamePins, out bool conficting, out TrackedObject foundConflict)
        {
            conficting = false;
            foundConflict = null;

            // attempt to change key
            if (!m_trackedObjects.AltDictionary.ContainsKey(objToEdit.ObjectID.ToLower()))
            {
                Debug.Error("Tracked object id not found report this to the dev");
                return false;
            }
            bool changeKeySuccess = m_trackedObjects.ChangeKey(
                key: objToEdit.ObjectID,
                newKey: newValues.ObjectID,
                out conficting,
                out foundConflict
                );
            if (!changeKeySuccess)
            {
                Debug.Warning("New ObjectID already exists, will not modify");
                return false;
            }

            // modify values
            m_trackedObjects.Modify(
                key: newValues.ObjectID,
                newValues,
                newNodeExact: newValues.IsExactMatchOnly,
                newBlackListWord: newValues.BlackListWords
                );

            OnTrackedObjectUpdate?.Invoke(objToEdit, newValues, renamePins);
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
            m_trackedObjects.SortTrackedObjects();
            string json = JsonConvert.SerializeObject(
                m_trackedObjects,
                new JsonSerializerSettings()
                {
                    FloatParseHandling = FloatParseHandling.Decimal,        // to prevent crashing when parsing colors which has more than 2 decimal places
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,   // to prevent exception when serializing color to prevent serializing reference loop like static colors
                });
            Debug.Log("Serialized, saving to data");
            ModConfig.Instance.TrackedObjectsConfig.Value = json;
            Debug.Log("Saved");
            OnTrackedObjectSaved?.Invoke(m_trackedObjects);
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
                        new JsonSerializerSettings()
                        {
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
                    Debug.Error(e);
                    message = (TextType.TRACKED_OBJECTS_INVALID);
                }
            }
            Debug.Log(message);
            OnTrackedObjectsReload?.Invoke(m_trackedObjects);
        }

    }
}