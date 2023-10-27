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
    internal static class GUIManagerExtension
    {
        // force grab private GUIInStart field
        private static readonly FieldInfo privateFieldInfoGUIInStart = typeof(GUIManager).GetField("GUIInStart", BindingFlags.NonPublic | BindingFlags.Instance);

        private static bool GUIInStart;
        public static TMP_FontAsset AveriaSerif { get; set; }
        public static TMP_FontAsset TMPNorse { get; set; }

        public static readonly List<Sprite> m_mapIcons = new List<Sprite>();

        public static void InitialTMPLoad(this GUIManager @this, Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name != "start") return;

            TMP_FontAsset[] source2 = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            AveriaSerif = source2.FirstOrDefault((TMP_FontAsset x) => x.name == "Valheim-AveriaSerifLibre");
            TMPNorse = source2.FirstOrDefault((TMP_FontAsset x) => x.name == "Valheim-Norse");

            if (AveriaSerif == null || TMPNorse == null)
            {
                throw new Exception("Fonts not found");
            }
            SceneManager.sceneLoaded -= @this.InitialTMPLoad;
        }

        public static void UpdateGUIInStart(this GUIManager @this)
        {
            GUIInStart = (bool)privateFieldInfoGUIInStart.GetValue(@this);
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
        //     AveriaSerif
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
        public static void ApplyTMPTextStyle(this GUIManager @this, TMP_Text text, TMP_FontAsset font, Color color, int fontSize = 16, bool createOutline = true)
        {
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = FontStyles.Bold;

            if (createOutline)
            {
                Outline orAddComponent = text.gameObject.GetOrAddComponent<Outline>();
                orAddComponent.effectColor = Color.black;
            }
        }

        //
        // Summary:
        //     Apply Valheim style to a TMPro.TMP_Text Component. Uses
        //     AveriaSerif
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
        public static void ApplyTMPTextStyle(this GUIManager @this, TMP_Text text, Color color, int fontSize = 16, bool createOutline = true)
        {
            @this.ApplyTMPTextStyle(text, AveriaSerif, color, fontSize, createOutline);
        }

        //
        // Summary:
        //     Apply Valheim style to a TMPro.TMP_Text Component. Uses
        //     AveriaSerif
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
        public static void ApplyTMPTextStyle(this GUIManager @this, TMP_Text text, int fontSize = 16)
        {
            @this.ApplyTMPTextStyle(text, AveriaSerif, Color.white, fontSize);
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
        public static void ApplyTMPButtonStyle(this GUIManager @this, Button button, int fontSize = 16)
        {
            @this.UpdateGUIInStart();

            GameObject gameObject = button.gameObject;
            Image component = gameObject.GetComponent<Image>();
            if ((bool)component)
            {
                component.sprite = @this.GetSprite("button");
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
            gameObject.GetComponent<Button>().colors = @this.ValheimButtonColorBlock;
            TMP_Text componentInChildren = gameObject.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if ((bool)componentInChildren)
            {
                @this.ApplyTMPTextStyle(componentInChildren, @this.ValheimOrange, fontSize);
                componentInChildren.alignment = TextAlignmentOptions.Center;
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
        public static void ApplyTMPInputFieldStyle(this GUIManager @this, TMP_InputField field)
        {
            @this.ApplyTMPInputFieldStyle(field, 16);
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
        public static void ApplyTMPInputFieldStyle(this GUIManager @this, TMP_InputField field, int fontSize = 16)
        {
            @this.UpdateGUIInStart();
            if (field.targetGraphic is Image image)
            {
                image.color = Color.white;
                image.sprite = @this.GetSprite("text_field");
                image.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            if (field.placeholder is TMP_Text text)
            {
                text.font = AveriaSerif;
                text.fontStyle = FontStyles.Bold | FontStyles.Italic;
                text.color = Color.grey;
                text.fontSize = fontSize;
            }

            if ((bool)field.textComponent)
            {
                @this.ApplyTMPTextStyle(field.textComponent, fontSize);
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
        public static void ApplyTMPDropdownStyle(this GUIManager @this, TMP_Dropdown dropdown, int fontSize = 16)
        {
            @this.UpdateGUIInStart();
            dropdown.gameObject.layer = 5;
            if (dropdown.template != null)
            {
                dropdown.template.gameObject.layer = 5;
            }

            if ((bool)dropdown.captionText)
            {
                @this.ApplyTMPTextStyle(dropdown.captionText, fontSize);
                // dropdown.captionText.verticalOverflow = VerticalWrapMode.Overflow; // todo: I think overflow and verticaloverflow is different, tmp doesn't have vertical overflow
            }

            if ((bool)dropdown.itemText)
            {
                @this.ApplyTMPTextStyle(dropdown.itemText, fontSize);
                // dropdown.captionText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (dropdown.TryGetComponent<Image>(out var component))
            {
                component.sprite = @this.GetSprite("text_field");
                component.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            GameObject gameObject = dropdown.transform.Find("Arrow").gameObject;
            gameObject.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 180f));
            if (gameObject.TryGetComponent<Image>(out var component2))
            {
                gameObject.SetSize(25f, 25f);
                component2.sprite = @this.GetSprite("map_marker");
                component2.color = Color.white;
                component2.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            if ((bool)dropdown.template && dropdown.template.TryGetComponent<ScrollRect>(out var component3))
            {
                @this.ApplyScrollRectStyle(component3);
            }

            if ((bool)dropdown.template && dropdown.template.TryGetComponent<Image>(out var component4))
            {
                component4.sprite = @this.GetSprite("button_small");
                component4.color = Color.white;
                component4.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
            }

            GameObject gameObject2 = dropdown.template.Find("Viewport/Content/Item").gameObject;
            if ((bool)gameObject2 && gameObject2.TryGetComponent<Toggle>(out var component5))
            {
                component5.toggleTransition = Toggle.ToggleTransition.None;
                component5.colors = @this.ValheimToggleColorBlock;
                component5.spriteState = new SpriteState
                {
                    highlightedSprite = @this.GetSprite("button_highlight")
                };
                if (component5.targetGraphic is Image image)
                {
                    image.enabled = false;
                }

                if (component5.graphic is Image image2)
                {
                    image2.sprite = @this.GetSprite("checkbox_marker");
                    image2.color = Color.white;
                    image2.type = Image.Type.Simple;
                    image2.maskable = true;
                    image2.pixelsPerUnitMultiplier = (GUIInStart ? 2f : 1f);
                    image2.gameObject.GetOrAddComponent<Outline>().effectColor = Color.black;
                }
            }
        }
    }
}