using Jotunn;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WxAxW.PinAssistant.Utils
{
    internal class TMPGUIManager
    {
        public static TMPGUIManager m_instance;
        public static TMPGUIManager Instance => m_instance ?? (m_instance = new TMPGUIManager());

        // force grab private GUIInStart field
        private readonly FieldInfo privateFieldInfoGUIInStart = typeof(GUIManager).GetField("GUIInStart", BindingFlags.NonPublic | BindingFlags.Instance);

        private bool GUIInStart;
        public TMP_FontAsset AveriaSerif { get; set; }
        public TMP_FontAsset AveriaSerifBold { get; set; }
        public TMP_FontAsset Norse { get; set; }
        public TMP_FontAsset NorseBold { get; set; }

        private ColorBlock ValheimButtonColorBlock { get; set; }
        private Color ValheimOrange { get; set; }
        private ColorBlock ValheimToggleColorBlock { get; set; }

        public readonly List<Sprite> m_mapIcons = new List<Sprite>();

        public void Init(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name != "start") return;

            TMP_FontAsset[] source2 = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            AveriaSerif = source2.FirstOrDefault((TMP_FontAsset x) => x.name == "Valheim-AveriaSerifLibre");
            AveriaSerifBold = source2.FirstOrDefault((TMP_FontAsset x) => x.name == "AveriaSerifLibre-Bold SDF");
            Norse = source2.FirstOrDefault((TMP_FontAsset x) => x.name == "Valheim-Norse");
            NorseBold = source2.FirstOrDefault((TMP_FontAsset x) => x.name == "Valheim-Norsebold");
            if (AveriaSerifBold == null || AveriaSerif == null || Norse == null || NorseBold == null)
            {
                throw new Exception("Fonts not found");
            }

            // retrieve data from GUIManager
            ValheimButtonColorBlock = GUIManager.Instance.ValheimButtonColorBlock;
            ValheimOrange = GUIManager.Instance.ValheimOrange;
            ValheimToggleColorBlock = GUIManager.Instance.ValheimToggleColorBlock;
        }

        public void UpdateGUIInStart()
        {
            GUIInStart = (bool)privateFieldInfoGUIInStart.GetValue(GUIManager.Instance);
        }

        public void ApplyWoodpanel(Image image)
        {
            image.sprite = GUIManager.Instance.GetSprite("woodpanel_trophys");
        }

        //
        // Summary:
        //     Apply Valheim style to a TMPro Component
        //
        // Parameters:
        //   text:
        //     Target component
        //
        //   font:
        //     Own font or
        //     AveriaSerifBold
        //     /
        //     AveriaSerif
        //
        //   color:
        //     Custom color or
        //     ValheimOrange
        //
        //   createOutline:
        //     creates an UnityEngine.UI.Outline component when true
        //
        //   fontSize:
        //     Optional font size, defaults to 16
        public void ApplyTextStyle(TMP_Text text, TMP_FontAsset font, Color color, int fontSize = 16, bool createOutline = true)
        {
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            if (createOutline)
            {
                Outline orAddComponent = text.gameObject.GetOrAddComponent<Outline>();
                orAddComponent.effectColor = Color.black;
            }
        }

        //
        // Summary:
        //     Apply Valheim style to a TMPro.TMP_Text Component. Uses
        //     AveriaSerifBold
        //     by default
        //
        // Parameters:
        //   text:
        //     Target component
        //
        //   color:
        //     Custom color or
        //     ValheimOrange
        //
        //   createOutline:
        //     creates an UnityEngine.UI.Outline component when true
        //
        //   fontSize:
        //     Optional font size, defaults to 16
        public void ApplyTextStyle(TMP_Text text, Color color, int fontSize = 16, bool createOutline = true)
        {
            ApplyTextStyle(text, AveriaSerifBold, color, fontSize, createOutline);
        }

        //
        // Summary:
        //     Apply Valheim style to a TMPro.TMP_Text Component. Uses
        //     AveriaSerifBold
        //     ,
        //     Color.white
        //     and creates an outline by default
        //
        // Parameters:
        //   text:
        //     Target component
        //
        //   fontSize:
        //     Optional font size, defaults to 16
        public void ApplyTextStyle(TMP_Text text, int fontSize = 16)
        {
            ApplyTextStyle(text, AveriaSerifBold, Color.white, fontSize);
        }

        //
        // Summary:
        //     Apply valheim style to a UnityEngine.UI.Button Component
        //
        // Parameters:
        //   button:
        //     Component to apply the style to
        //
        //   fontSize:
        //     Optional font size, defaults to 16
        public void ApplyButtonStyle(Button button, int fontSize = 16)
        {
            UpdateGUIInStart();

            GameObject gameObject = button.gameObject;
            Image component = gameObject.GetComponent<Image>();
            if ((bool)component)
            {
                component.sprite = GUIManager.Instance.GetSprite("button");
                component.type = Image.Type.Sliced;
                component.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
                button.image = component;
            }

            if (!gameObject.TryGetComponent<ButtonSfx>(out var component2))
            {
                component2 = gameObject.AddComponent<ButtonSfx>();
            }

            component2.m_sfxPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_gui_button");
            component2.m_selectSfxPrefab = PrefabManager.Cache.GetPrefab<GameObject>("sfx_gui_select");
            gameObject.GetComponent<Button>().colors = ValheimButtonColorBlock;
            TMP_Text componentInChildren = gameObject.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if ((bool)componentInChildren)
            {
                ApplyTextStyle(componentInChildren, ValheimOrange, fontSize);
                componentInChildren.alignment = TextAlignmentOptions.Center; // todo: double check if center means middle center
            }
        }

        //
        // Summary:
        //     Apply Valheim style to an TMPro.TMP_InputField Component.
        //
        // Parameters:
        //   field:
        //     Component to apply the style to
        [Obsolete("Only here for backward compat")]
        public void ApplyInputFieldStyle(TMP_InputField field)
        {
            ApplyInputFieldStyle(field, 16);
        }

        //
        // Summary:
        //     Apply Valheim style to an TMPro.TMP_InputField Component.
        //
        // Parameters:
        //   field:
        //     Component to apply the style to
        //
        //   fontSize:
        //     Optional font size, defaults to 16
        public void ApplyInputFieldStyle(TMP_InputField field, int fontSize = 16)
        {
            UpdateGUIInStart();
            if (field.targetGraphic is Image image)
            {
                image.color = Color.white;
                image.sprite = GUIManager.Instance.GetSprite("text_field");
                image.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            if (field.placeholder is TMP_Text text)
            {
                text.font = AveriaSerifBold;
                text.color = Color.grey;
                text.fontSize = fontSize;
            }

            if ((bool)field.textComponent)
            {
                ApplyTextStyle(field.textComponent, AveriaSerif, Color.white, fontSize);
            }
        }

        //
        // Summary:
        //     Apply Valheim style to a UnityEngine.UI.Dropdown component.
        //
        // Parameters:
        //   dropdown:
        //     Component to apply the style to
        //
        //   fontSize:
        //     Optional font size, defaults to 16
        public void ApplyDropdownStyle(TMP_Dropdown dropdown, int fontSize = 16)
        {
            UpdateGUIInStart();
            dropdown.gameObject.layer = 5;
            if (dropdown.template != null)
            {
                dropdown.template.gameObject.layer = 5;
            }

            if ((bool)dropdown.captionText)
            {
                ApplyTextStyle(dropdown.captionText, fontSize);
                // dropdown.captionText.verticalOverflow = VerticalWrapMode.Overflow; // todo: I think overflow and verticaloverflow is different, tmp doesn't have vertical overflow
            }

            if ((bool)dropdown.itemText)
            {
                ApplyTextStyle(dropdown.itemText, fontSize);
                // dropdown.captionText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (dropdown.TryGetComponent<Image>(out var component))
            {
                component.sprite = GUIManager.Instance.GetSprite("text_field");
                component.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            GameObject gameObject = dropdown.transform.Find("Arrow").gameObject;
            gameObject.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
            if (gameObject.TryGetComponent<Image>(out var component2))
            {
                gameObject.SetSize(25f, 25f);
                component2.sprite = GUIManager.Instance.GetSprite("map_marker");
                component2.color = Color.white;
                component2.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            if ((bool)dropdown.template && dropdown.template.TryGetComponent<ScrollRect>(out var component3))
            {
                GUIManager.Instance.ApplyScrollRectStyle(component3);
            }

            if ((bool)dropdown.template && dropdown.template.TryGetComponent<Image>(out var component4))
            {
                component4.sprite = GUIManager.Instance.GetSprite("button_small");
                component4.color = Color.white;
                component4.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            GameObject gameObject2 = dropdown.template.Find("Viewport/Content/Item").gameObject;
            if ((bool)gameObject2 && gameObject2.TryGetComponent<Toggle>(out var component5))
            {
                component5.toggleTransition = Toggle.ToggleTransition.None;
                component5.colors = ValheimToggleColorBlock;
                component5.spriteState = new SpriteState
                {
                    highlightedSprite = GUIManager.Instance.GetSprite("button_highlight")
                };
                if (component5.targetGraphic is Image image)
                {
                    image.enabled = false;
                }

                if (component5.graphic is Image image2)
                {
                    image2.sprite = GUIManager.Instance.GetSprite("checkbox_marker");
                    image2.color = Color.white;
                    image2.type = Image.Type.Simple;
                    image2.maskable = true;
                    image2.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
                    image2.gameObject.GetOrAddComponent<Outline>().effectColor = Color.black;
                }
            }
        }

        /*
        public static void ApplyAllSunken(Transform root)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>())
            {
                if (image.gameObject.name == "Sunken")
                {
                    image.sprite = GUIManager.Instance.GetSprite("sunken");
                    image.color = Color.white;
                    image.type = Image.Type.Sliced;
                    image.pixelsPerUnitMultiplier = 1;
                }
            }
        }
        public static void ApplyStyle(Transform root)
        {
            ApplyWoodpanel(root.GetChild(0).GetComponent<Image>());

            foreach (Text text in root.GetComponentsInChildren<Text>())
            {
                ApplyText(text, GUIManager.Instance.AveriaSerif, new Color(219f / 255f, 219f / 255f, 219f / 255f));
            }

            foreach (InputField inputField in root.GetComponentsInChildren<InputField>())
            {
                GUIManager.Instance.ApplyInputFieldStyle(inputField, 16);
            }

            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>())
            {
                GUIManager.Instance.ApplyToogleStyle(toggle);
            }

            foreach (Button button in root.GetComponentsInChildren<Button>())
            {
                GUIManager.Instance.ApplyButtonStyle(button);
            }

            foreach (Dropdown dropdown in root.GetComponentsInChildren<Dropdown>())
            {
                GUIManager.Instance.ApplyDropdownStyle(dropdown);
            }
        }
        */
    }
}