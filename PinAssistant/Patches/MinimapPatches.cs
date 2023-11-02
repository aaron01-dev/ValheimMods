using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using WxAxW.PinAssistant.Configuration;
using WxAxW.PinAssistant.Core;
using Debug = WxAxW.PinAssistant.Utils.Debug;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace WxAxW.PinAssistant.Patches
{
    [HarmonyPatch(typeof(Minimap))]
    internal class MinimapPatches
    {
        public static event Action<Minimap.PinData> OnPinAdd;
        public static event Action<Minimap.PinData> OnPinRemove;
        public static event Action OnPinClear;
        public static event Action<Minimap.PinData> OnSetTargetPin;
        public static event Action OnUpdatePin;

        public static bool isSpecialPin = false;

        private static FieldInfo AccessMinimapField(string field)
        {
            return AccessTools.Field(typeof(Minimap), field);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.AddPin))]
        private static void Postfix(ref Minimap.PinData __result) // get the return value of AddPin to add to plugin's pins dictionary
        {
            OnPinAdd?.Invoke(__result);
            isSpecialPin = false;
        }

        // patches the specific function RemovePin(PinData pin) inside the Minimap Class
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.RemovePin), new Type[] { typeof(Minimap.PinData) })]
        private static void Prefix(ref Minimap.PinData pin)
        {
            OnPinRemove?.Invoke(pin);
        }

        // patch to clear the plugin's m_pins as well
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.ClearPins))]
        private static void PrefixClearPins()
        {
            OnPinClear?.Invoke();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.UpdatePins))]
        private static void PostFixUpdatePins()
        {
            if (!ModConfig.Instance.IsEnabledConfig.Value) return;
            MinimapAssistant.Instance.FilterPins();
            MinimapAssistant.Instance.ColorPins();
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Minimap.ShowPinNameInput))]
        private static void PostFixShowPinNameInput(Minimap __instance, ref bool __runOriginal)
        {
            if (!__runOriginal) return;

            Debug.Log("New manual pin added");
            SetTargetPin(__instance.m_namePin);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Minimap.HidePinTextInput))]
        private static void PrefixHidePinTextInput(Minimap __instance)
        {
            if (__instance.m_namePin == null) return;
            Debug.Log("New manual pin editted");
            UpdatePin();
        }

        public static void SetTargetPin(Minimap.PinData pin)
        {
            OnSetTargetPin?.Invoke(pin);
        }

        public static void UpdatePin()
        {
            OnUpdatePin?.Invoke();
        }

        // ignore special pins
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Minimap.DiscoverLocation))] // ignore newly discovered pin
        [HarmonyPatch(nameof(Minimap.UpdateProfilePins))] // ignore spawn point pin
        [HarmonyPatch(nameof(Minimap.UpdateEventPin))] // ignore event pin (the red circle and exclamation point) | has 2 add pin but exclude method covers that by continuously repeating till matcher doesn't find anything anymore
        [HarmonyPatch(nameof(Minimap.UpdateLocationPins))] // ignore boss pins, merchant, others
        [HarmonyPatch(nameof(Minimap.UpdatePlayerPins))] // ignore player pins (yourself, and others?)
        [HarmonyPatch(nameof(Minimap.UpdatePingPins))] // ignore ping pins (Ping when middle clicking minimap)
        [HarmonyPatch(nameof(Minimap.UpdateShoutPins))] // ignore shout pins (I have Arrived!)
        private static IEnumerable<CodeInstruction> TranspilerIgnoreNewPin(IEnumerable<CodeInstruction> instructions)
        {
            return ExcludePinsInMethod(instructions);
        }

        public static IEnumerable<CodeInstruction> ExcludePinsInMethod(IEnumerable<CodeInstruction> instructions, bool isVirtual = false)
        {
            /* with emit delegate
            return FindBeforeAddPin(instructions)
                .Insert(
                    new CodeInstruction(Transpilers.EmitDelegate<Func<bool>>(SetIsSpecialPin)),
                    new CodeInstruction(OpCodes.Pop)    // remove the return value of SetIsSpecialPin
                )
                .InstructionEnumeration();
            */
            // without emit delegate
            return FindBeforeAddPin(instructions, isVirtual)
                .Repeat(
                    matcher =>
                    {
                        matcher.InsertAndAdvance(
                            new CodeInstruction(OpCodes.Ldc_I4_1), // add 1 (true) to stack
                            new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MinimapPatches), nameof(isSpecialPin)))
                        )
                        .Advance(1); // advance to after AddPin Method
                    }
                )
                .InstructionEnumeration();
        }

        private static CodeMatcher FindBeforeAddPin(IEnumerable<CodeInstruction> instructions, bool isVirtual)
        {
            return new CodeMatcher(instructions)
                .MatchForward(
                    false,
                    new CodeMatch(isVirtual ? OpCodes.Callvirt : OpCodes.Call, AccessTools.Method(typeof(Minimap), nameof(Minimap.AddPin)))
                    );
        }
    }
}